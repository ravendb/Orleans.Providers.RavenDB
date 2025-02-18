. "$PSScriptRoot/checkLastExitCode.ps1"

function CreateNugetPackage ($srcDir, $targetFilename, $versionSuffix) {
    dotnet pack --output $targetFilename `
        --configuration "Release" `
        --version-suffix $versionSuffix `
        $srcDir

    CheckLastExitCode
}
