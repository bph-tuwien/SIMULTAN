if ($null -eq $Env:GIT_CRED_PSW -or '' -eq $Env:GIT_CRED_PSW) {
    Write-Host("Environment variable GIT_CRED_PSW not set, aborting.")
    exit -1
}

Write-Host("Starting Deployment...")

Write-Host("Getting DLL version...")

# Set TLS and SSL versions cause Jenkins needs it
[Net.ServicePointManager]::SecurityProtocol = "Tls12, Tls11, Tls, Ssl3"
# Path to SIMULTAN DLL
$path = Join-Path $(Get-Location).Path 'SIMULTAN\bin\Release\SIMULTAN.dll'
# Load the assembly into memory so we don't get a file lock
$assembly = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($path))
$ver = $assembly.GetName().Version
$version = "{0}.{1}.{2}" -f $ver.Major, $ver.Minor, $ver.Build

Write-Host("DLL version is $version")

Write-Host("Creating release...")

# Make the GitHub release, push DLL later
try
{
    $response = Invoke-WebRequest -Method 'POST' -Headers @{'Accept' ='application/vnd.github.v3+json';'Authorization' = "token $Env:GIT_CRED_PSW";} `
        -Body "{""tag_name"": ""$version"", ""target_commitish"": ""main"", ""name"": ""Release version $version""}" `
        -Uri "https://api.github.com/repos/bph-tuwien/$Env:REPO_NAME/releases"
}
catch {
    $StatusCode = $_.Exception.Response.StatusCode.value__
    if( 422 -eq $StatusCode)
    {
        Write-Host("Release already exists, aborting.")
    }
    else {
        Write-Host("An unknown error occured during release creation, aborting.")
        $_.Exception
    }
    exit -1
}

# Get release id from response
$parsed = ConvertFrom-Json($response.Content)
$id = $parsed.id

Write-Host("Created release with id $id")

# Find nuget package to upload
$nupkgs = Get-ChildItem -File -Path .\SIMULTAN\ "*$version*.nupkg"

try
{
    # Push DLL to release assets
    Write-Host("Uploading DLL...")
    $response = Invoke-WebRequest -Method 'POST' -Headers @{'Accept' ='application/vnd.github.v3+json';'Authorization' = "token $Env:GIT_CRED_PSW";} `
        -InFile $path -ContentType 'applicateion/octet-stream' `
        -Uri "https://uploads.github.com/repos/bph-tuwien/$Env:REPO_NAME/releases/$id/assets?name=SIMULTAN.dll"

    # See if we found some nuget packages
    if ( $nupkgs.Length -gt 0)
    {
        Write-Host("Uploading Nuget package...")
        $response = Invoke-WebRequest -Method 'POST' -Headers @{'Accept' ='application/vnd.github.v3+json';'Authorization' = "token $Env:GIT_CRED_PSW";} `
            -InFile $nupkgs[0].FullName -ContentType 'applicateion/octet-stream' `
            -Uri "https://uploads.github.com/repos/bph-tuwien/$Env:REPO_NAME/releases/$id/assets?name=$($nupkgs[0].Name)"
    }
    else {
        Write-Host("No Nuget package found, skipping upload.")
    }

    # upload docs if any
    if (Test-Path ".\docfx_project\_site")
    {
        Write-Host("Zipping docs...")
        Compress-Archive ".\docfx_project\_site\" "docs.zip"
        Write-Host("Uploading docs...")
        $response = Invoke-WebRequest -Method 'POST' -Headers @{'Accept' ='application/vnd.github.v3+json';'Authorization' = "token $Env:GIT_CRED_PSW";} `
            -InFile "docs.zip" -ContentType 'applicateion/octet-stream' `
            -Uri "https://uploads.github.com/repos/bph-tuwien/$Env:REPO_NAME/releases/$id/assets?name=docs.zip"
    }
    else {
        Write-Host("No docs found, skipping upload.")
    }

}
catch
{
    Write-Host("An unknown error occured during DLL upload, aborting.")
    $_.Exception
    exit -1
}

Write-Host("Release done.")