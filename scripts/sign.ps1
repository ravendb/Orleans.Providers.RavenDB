. "$PSScriptRoot/checkLastExitCode.ps1"

function SignFile ($filePath, $dryRun) {
    if ($dryRun) {
        Write-Host "[DRY RUN] Sign file $filePath..."
        return
    }

    $signToolPaths = @(
        "C:\Program Files (x86)\Windows Kits\8.1\bin\x64\signtool.exe",
        "C:\Program Files (x86)\Windows Kits\8.0\bin\x86\signtool.exe",
        "C:\Program Files\Microsoft SDKs\Windows\v7.1\Bin\signtool.exe",
        "C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe"
    )

    $signTool = $signToolPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (!$signTool) {
        throw "SignTool.exe not found."
    }

    $installerCert = $env:CODESIGN_CERTPATH
    if (!(Test-Path $installerCert)) {
        throw "Could not find pfx file at $installerCert"
    }

    $certPasswordPath = $env:CODESIGN_PASSPATH
    if (!(Test-Path $certPasswordPath)) {
        throw "Could not find certificate password file at $certPasswordPath"
    }

    $certPassword = Get-Content $certPasswordPath
    if ($certPassword -eq $null) {
        throw "Certificate password is required"
    }

    Write-Host "Signing: $filePath"
    & $signTool sign /f "$installerCert" /p "$certPassword" /fd SHA256 `
        /d "RavenDB-Orleans" /du "https://ravendb.net" `
        /tr "http://timestamp.digicert.com" /td SHA256 /v "$filePath"

    CheckLastExitCode
}
