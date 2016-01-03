#
# Adds the build number to the project.json version for 
# continuous delivery to NuGet/MyGet.
#

#$ErrorActionPreference = "Stop" 

if ($true) {
    # for testing the script locally
    $env:APPVEYOR_REPO_TAG = $false
    $env:APPVEYOR_BUILD_NUMBER = 1234
}

if (!$env:APPVEYOR_BUILD_NUMBER) {
    Write-Host "ERROR: APPVEYOR_BUILD_NUMBER is not set! Exiting"
    exit
}

if ($env:APPVEYOR_REPO_TAG -eq $true -or $env:APPVEYOR_REPO_TAG -eq "true") {
    Write-Host "INFO: Build running from tag - project.json will not be changed."
    exit
}

# Change version in every project.json
Get-ChildItem -Filter project.json -Recurse | ForEach-Object {
    $json = (Get-Content -Raw -Path $_.FullName | ConvertFrom-Json)

    $originalVersion = $json.version
    $newVersion = ($json.version).TrimEnd('*', '-') + "-" + $env:APPVEYOR_BUILD_NUMBER

    $json.version = $newVersion
    
    $json | ConvertTo-Json | Set-Content $_.FullName

    Write-Host "DEBUG: Changed " + $_.FullName + " from $originalVersion to $newVersion"
}