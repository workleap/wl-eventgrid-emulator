#Requires -Version 5.0

Begin {
    $ErrorActionPreference = "stop"
}

Process {
    function Exec([scriptblock]$Command) {
        & $Command
        if ($LASTEXITCODE -ne 0) {
            throw ("An error occurred while executing command: {0}" -f $Command)
        }
    }
    
    $workingDir = Join-Path $PSScriptRoot "src"
    $outputDir = Join-Path $PSScriptRoot ".output"
    $nupkgsPath = Join-Path $outputDir "*.nupkg"

    try {
        Push-Location $workingDir
        Remove-Item $outputDir -Force -Recurse -ErrorAction SilentlyContinue
    
        Exec { & dotnet clean -c Release }
        Exec { & dotnet build -c Release }
        Exec { & dotnet pack "EventGridEmulator/EventGridEmulator.csproj" -c Release -o "$outputDir" --no-build }
        Exec { & dotnet test  -c Release --results-directory "$outputDir" --no-restore -l "trx" -l "console;verbosity=detailed" }
    }
    finally {
        Pop-Location
    }

    # Publish NuGet packages if environment variables are set
    if (($null -ne $env:NUGET_SOURCE ) -and ($null -ne $env:VSS_NUGET_ACCESSTOKEN)) {
        Exec { & dotnet nuget push "$nupkgsPath" -s $env:NUGET_SOURCE -k "az-api-key" --skip-duplicate }
    }
}