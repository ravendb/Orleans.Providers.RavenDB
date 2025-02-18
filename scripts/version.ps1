$RELEASE_INFO_FILE = "artifacts/release-info.json"

function SetVersionInfo($projectDir) {
    $builtAt = Get-Date -Format "yyyyMMdd-HHmm"
    $version = GetCurrentVersionPrefix $projectDir

    $versionInfo = @{
        Version       = $version
        BuiltAt       = (Get-Date).ToUniversalTime()
        BuiltAtString = $builtAt
    }

    New-Item -Path $RELEASE_INFO_FILE -Force -Type File | Out-Null
    $versionInfoJson = ConvertTo-Json -InputObject $versionInfo
    Set-Content -Path $RELEASE_INFO_FILE -Value $versionInfoJson

    return $versionInfo
}

function GetVersionInfo() {
    return Get-Content -Path $RELEASE_INFO_FILE | ConvertFrom-Json
}

function BumpVersion ($projectDir, $versionPrefix, $buildType) {
    if ($buildType.ToLower() -ne "stable") {
        return
    }

    Write-Host "Calculating new version..."
    $newVersion = SemverMinor $versionPrefix
    Write-Host "New version: $newVersion"

    $remoteFilePath = "src/CommonAssemblyInfo.cs"
    $assemblyInfoFile = Join-Path $projectDir -ChildPath $remoteFilePath
    UpdateVersionInFile $assemblyInfoFile $newVersion
}

function UpdateVersionInFile ($file, $version) {
    $versionPattern = [regex]'\[assembly: AssemblyVersion\(".*"\)\]'
    $result = [System.IO.File]::ReadAllText($file)
    $result = $versionPattern.Replace($result, "[assembly: AssemblyVersion(""$version"")]")
    [System.IO.File]::WriteAllText($file, $result, [System.Text.Encoding]::UTF8)
}

function GetCurrentVersionPrefix($projectDir) {
    $commonAssemblyInfoFile = Join-Path $projectDir "src/CommonAssemblyInfo.cs"
    $match = Select-String -Path $commonAssemblyInfoFile -Pattern 'AssemblyVersion\("(.*)"\)'
    return $match.Matches.Groups[1].Value
}

function SemverMinor ($versionPrefix) {
    $versionStrings = $versionPrefix -split '\.'
    $versionNumbers = $versionStrings | ForEach-Object { [int]$_ }
    $versionNumbers[2] += 1
    return $versionNumbers -join "."
}
