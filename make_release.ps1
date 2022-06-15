# Path to SIMULTAN DLL
$path = Join-Path $(Get-Location).Path 'SIMULTAN\bin\Release\SIMULTAN.dll'
# Load the assembly into memory so we don't get a file lock
$assembly = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($path))
$version = $assembly.GetName().Version.ToString()

# Make the GitHub release, push DLL later
$response = Invoke-WebRequest -Method 'POST' -Headers @{'Accept' ='application/vnd.github.v3+json';'Authorization' = "token $Env:GIT_CRED";} `
    -Body "{""tag_name"": ""$version"", ""target_commitish"": ""main"", ""name"": ""Release version $version""}" `
    -Uri https://api.github.com/repos/bph-tuwien/SIMULTAN.BPH/releases

# Get release id from response
$parsed = ConvertFrom-Json($response.Content)
$id = $parsed.id

# Push DLL to release assets
Invoke-WebRequest -Method 'POST' -Headers @{'Accept' ='application/vnd.github.v3+json';'Authorization' = "token $Env:GIT_CRED";} `
    -InFile $path -ContentType 'applicateion/octet-stream' `
    -Uri "https://uploads.github.com/repos/bph-tuwien/SIMULTAN.BPH/releases/$id/assets?name=SIMULTAN.dll"