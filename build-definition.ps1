Include "build-helpers.ps1"

Properties {

    # This number will be appended to all nuget package versions and to the service fabric app versions
    # This should be overwritten by a CI system like VSTS, AppVeyor, TeamCity, ...
    $BuildNumber = "local-" + ((Get-Date).ToUniversalTime().ToString("MMddHHmm"))

    # The build configuration used for compilation
    $BuildConfiguration = "Release"

    # The folder in which all output packages should be placed
    $ArtifactsPath = Join-Path $PWD "artifacts"

    # Artifacts-subfolder in which test results will be placed
    $ArtifactsPathTests = "tests"

    # Artifacts-subfolder in which NuGet packages will be placed
    $ArtifactsPathNuGet = "nuget"

    # A list of projects for which NuGet packages should be created
    $NugetLibraries = @( `
        "src/C3.ServiceFabric.AspNetCore.StatelessHost", `
        "src/C3.ServiceFabric.HttpCommunication", `
        "src/C3.ServiceFabric.HttpServiceGateway" )

    # A list of "Service Fabric" applications for which a deployment package should be created
    $ServiceFabricApps = @( "GatewaySample" )

    # Set this if your Service Fabric apps are placed in a subfolder
    $ServiceFabricBasePath = "samples"
}

FormatTaskName ("`n" + ("-"*25) + "[{0}]" + ("-"*25) + "`n")

Task Default -depends init, clean, dotnet-install, dotnet-build, dotnet-test, dotnet-pack, packageServiceFabric

Task init {

    Write-Host "BuildNumber: $BuildNumber"
    Write-Host "BuildConfiguration: $BuildConfiguration"
    Write-Host "ArtifactsPath: $ArtifactsPath"
    Write-Host "ArtifactsPathTests: $ArtifactsPathTests"
    Write-Host "ArtifactsPathNuGet: $ArtifactsPathNuGet"

    Assert ($BuildNumber -ne $null) "Property 'BuildNumber' may not be null."
    Assert ($BuildConfiguration -ne $null) "Property 'BuildConfiguration' may not be null."
    Assert ($ArtifactsPath -ne $null) "Property 'ArtifactsPath' may not be null."
    Assert ($ArtifactsPathTests -ne $null) "Property 'ArtifactsPathTests' may not be null."
    Assert ($ArtifactsPathNuGet -ne $null) "Property 'ArtifactsPathNuGet' may not be null."
}

Task clean {

    if (Test-Path $ArtifactsPath) { Remove-Item -Path $ArtifactsPath -Recurse -Force -ErrorAction Ignore }
    New-Item $ArtifactsPath -ItemType Directory -ErrorAction Ignore | Out-Null

    Write-Host "Created artifacts folder '$ArtifactsPath'"
}

Task dotnet-install {

    if (Get-Command "dotnet.exe" -ErrorAction SilentlyContinue) {
        Write-Host "dotnet SDK already installed"
        exec { dotnet --version }
    } else {
        Write-Host "Installing dotnet SDK"

        $installScript = Join-Path $ArtifactsPath "dotnet-install.ps1"

        Invoke-WebRequest "https://raw.githubusercontent.com/dotnet/cli/release/2.0.0/scripts/obtain/dotnet-install.ps1" `
            -OutFile $installScript

        & $installScript
    }
}

Task dotnet-build {

    # This is here as a workaround until there's no more classic nuget dependencies.
    # (e.g. Service Fabric applications)
    exec { nuget restore }

    $versionSuffixArg = if ([String]::IsNullOrWhiteSpace($BuildNumber)) { "" } else { "--version-suffix $BuildNumber" }

    # --no-incremental to ensure that CI builds always result in a clean build
    exec { Invoke-Expression "dotnet build -c $BuildConfiguration $versionSuffixArg --no-incremental --no-restore" }
}

Task dotnet-test {

    $testOutput = Join-Path $ArtifactsPath $ArtifactsPathTests
    New-Item $testOutput -ItemType Directory -ErrorAction Ignore | Out-Null

    $testsFailed = $false

    Get-ChildItem .\test -Filter *.csproj -Recurse | ForEach-Object {

        $library = Split-Path $_.DirectoryName -Leaf
        $testResultOutput = Join-Path $testOutput "$library.trx"

        Write-Host ""
        Write-Host "Testing $library"
        Write-Host ""

        dotnet test $_.FullName -c $BuildConfiguration --no-restore --no-build --logger "trx;LogFileName=$testResultOutput"
        if ($LASTEXITCODE -ne 0) {
            $testsFailed = $true
        }
    }

    if ($testsFailed) {
        throw "at least one test failed"
    }
}

Task dotnet-pack {

    if ($NugetLibraries -eq $null -or $NugetLibraries.Count -eq 0) {
        Write-Host "No NugetLibraries configured"
        return
    }

    $libraryOutput = Join-Path $ArtifactsPath $ArtifactsPathNuGet
    $versionSuffixArg = if ([String]::IsNullOrWhiteSpace($BuildNumber)) { "" } else { "--version-suffix $BuildNumber" }

    $NugetLibraries | ForEach-Object {

        $library = $_

        Write-Host ""
        Write-Host "Packaging $library to $libraryOutput"
        Write-Host ""

        exec { Invoke-Expression "dotnet pack $library -c $BuildConfiguration $versionSuffixArg --no-restore --no-build --include-source --include-symbols -o $libraryOutput" }
    }

    # HACK!! We want to include the PDB files in the regular nupkg so people can debug into them
    # without having to go through an (internal) symbol server
    Write-Host ""
    Write-Host "Replacing regular .nupkg files with .symbols.nupkg content"
    Get-ChildItem -Path $libraryOutput -Filter *.symbols.nupkg | ForEach-Object {

        $newName = $_.Name -replace ".symbols.nupkg", ".nupkg"
        $destination = Join-Path $_.Directory.FullName $newName

        Move-Item -Path $_.FullName -Destination $destination -Force
    }
}

Task packageServiceFabric {

    if ($ServiceFabricApps -eq $null -or $ServiceFabricApps.Count -eq 0) {
        Write-Host "No ServiceFabricApps configured"
        return
    }

    $ServiceFabricApps | ForEach-Object {

        $outputPath = Join-Path $ArtifactsPath $_

        PublishSfApp $ServiceFabricBasePath $_ $outputPath $BuildConfiguration $BuildNumber
    }
}