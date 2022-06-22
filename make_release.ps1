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
$version = $assembly.GetName().Version.ToString()

Write-Host("DLL version is $version")

Write-Host("Creating release...")

# Make the GitHub release, push DLL later
$response = Invoke-WebRequest -Method 'POST' -Headers @{'Accept' ='application/vnd.github.v3+json';'Authorization' = "token $Env:GIT_CRED_PSW";} `
    -Body "{""tag_name"": ""$version"", ""target_commitish"": ""main"", ""name"": ""Release version $version""}" `
    -Uri "https://api.github.com/repos/bph-tuwien/$Env:REPO_NAME/releases"

# Get release id from response
$parsed = ConvertFrom-Json($response.Content)
$id = $parsed.id

Write-Host("Created release with id $id")

Write-Host("Uploading DLL...")

# Push DLL to release assets
$response = Invoke-WebRequest -Method 'POST' -Headers @{'Accept' ='application/vnd.github.v3+json';'Authorization' = "token $Env:GIT_CRED_PSW";} `
    -InFile $path -ContentType 'applicateion/octet-stream' `
    -Uri "https://uploads.github.com/repos/bph-tuwien/$Env:REPO_NAME/releases/$id/assets?name=SIMULTAN.dll"

Write-Host("Release done.")