#
# Build Script helpers
#

function GetServicesFromSfApp($sfAppDirectory) {
    # Reads the "application" project file to find all referenced services.

    $projectFile = Get-ChildItem -Path $sfAppDirectory -Filter *.sfproj
    if ($projectFile -eq $null) {
        throw "No *.sfproj file found in directory $sfAppDirectory"
    }
    
    $references = Select-Xml -Path $projectFile.FullName -XPath "//*[local-name()='ProjectReference']"

    $references | ForEach-Object { 
        $str = $_.Node.Include.ToString().Trim(".\")
        Write-Output $str.Substring(0, $str.IndexOf("\"))
    }
}

function UpdateSfVersions($packageLocation, $versionSuffix) {
    # appends the given versionSuffix to all ServiceManifest.xml files and to the ApplicationManifest.xml

    Write-Host "Updating Service Fabric versions for $packageLocation"

    $appManifestPath = "$packageLocation\ApplicationManifest.xml"
    $appManifestXml = [XML](Get-Content $appManifestPath)
    $appManifestXml.ApplicationManifest.ApplicationTypeVersion += $versionSuffix
    $appManifestXml.ApplicationManifest.ServiceManifestImport | ForEach { $_.ServiceManifestRef.ServiceManifestVersion += $versionSuffix }
    $appManifestXml.Save($appManifestPath)

    "Updated application type '$($appManifestXml.ApplicationManifest.ApplicationTypeName)' to version '$($appManifestXml.ApplicationManifest.ApplicationTypeVersion)'"

    $serviceManifestPaths = [System.IO.Directory]::EnumerateFiles($packageLocation, "ServiceManifest.xml", [System.IO.SearchOption]::AllDirectories)
    $serviceManifestPaths | ForEach {
        $serviceManifestXml = [XML](Get-Content $_)
        $serviceManifestXml.ServiceManifest.Version += $versionSuffix
        $subPackages = @(
            $serviceManifestXml.ServiceManifest.CodePackage,
            $serviceManifestXml.ServiceManifest.ConfigPackage,
            $serviceManifestXml.ServiceManifest.DataPackage)
        $subPackages | Where { $_.Version } | ForEach { $_.Version += $versionSuffix }
        $serviceManifestXml.Save($_)
  
        Write-Host "Updated service '$($serviceManifestXml.ServiceManifest.Name)' to version '$($serviceManifestXml.ServiceManifest.Version)'"
    }
}

function PublishSfApp($sfBasePath, $sfApp, $outputPath, $buildConfiguration, $buildNumber) {
    # it copies all services, application files and scripts into an artifacts folder.
    
    $sfAppPath = Join-Path $sfBasePath $sfApp
    
    Write-Host "Publishing Service Fabric-App '$sfAppPath' to $outputPath"

    $sfServices = GetServicesFromSfApp $sfAppPath
    
    Write-Host "App contains the following services: $sfServices"
    Write-Host ""
        
    $applicationPackage = Join-Path $outputPath "ApplicationPackage"

    if(!(Test-Path $applicationPackage)) {
        New-Item $applicationPackage -ItemType Directory | Out-Null
    }

    $sfServices | ForEach-Object {

        $serviceName = $_
        $servicePath = Join-Path $sfBasePath $serviceName
        $serviceOutput = Join-Path $applicationPackage $serviceName

        Write-Host "Publishing $serviceName to $serviceOutput"
        
        $codeOutput = Join-Path $serviceOutput "Code"
        
        if(!(Test-Path $codeOutput)) {
            New-Item $codeOutput -ItemType Directory | Out-Null
        }
            
        exec { dotnet publish $servicePath -c $buildConfiguration --version-suffix $buildNumber --no-build -o $codeOutput }
                        
        Copy-Item (Join-Path $servicePath "PackageRoot/*") $serviceOutput -Recurse -Force
    }

    Write-Host "Publishing application artifacts"

    Copy-Item (Join-Path $sfAppPath "ApplicationPackageRoot/*") $applicationPackage -Recurse -Force
    
    # folders required for deployment
    Copy-Item (Join-Path $sfAppPath "ApplicationParameters") $outputPath -Recurse -Force
    Copy-Item (Join-Path $sfAppPath "PublishProfiles") $outputPath -Recurse -Force
    Copy-Item (Join-Path $sfAppPath "Scripts") $outputPath -Recurse -Force

    # Update version
    $packageLocation = (Get-Item $applicationPackage).FullName
    $versionSuffix = "-$buildNumber"
    UpdateSfVersions $packageLocation $versionSuffix
}