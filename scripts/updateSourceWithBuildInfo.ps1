. "$PSScriptRoot/checkLastExitCode.ps1"

function BuildProject ($srcDir) {
    Write-Host "Building Project..."
    & dotnet build /p:SourceLinkCreate=true /p:GenerateDocumentationFile=true `
        --no-incremental --configuration "Release" $srcDir
    CheckLastExitCode
}

function UpdateSourceWithBuildInfo ($projectDir, $version) {
    $commit = Get-Git-Commit-Short
    UpdateCommonAssemblyInfo $projectDir $version $commit
    UpdateCsprojAndNuspecWithVersionInfo $projectDir $version
}

function UpdateCsprojAndNuspecWithVersionInfo ($projectDir, $version) {
    Write-Host "Updating version in Directory.build.props..."
    
    $src = Join-Path $projectDir -ChildPath "src"
    $csprojFile = Join-Path $src -ChildPath "Orleans.Providers.RavenDb/Orleans.Providers.RavenDb.csproj"

    UpdateVersionInFile $csprojFile $version
}

function UpdateVersionInFile ($file, $version) {
    $versionPattern = [regex]'(?sm)<Version>[A-Za-z0-9-\.\r\n\s]*</Version>'
    $inputText = [System.IO.File]::ReadAllText($file)
    $result = $versionPattern.Replace($inputText, "<Version>$version</Version>")
    [System.IO.File]::WriteAllText($file, $result, [System.Text.Encoding]::UTF8)
}

function UpdateCommonAssemblyInfo ($projectDir, $version, $commit) {
    $assemblyInfoFile = Join-Path $projectDir "src/CommonAssemblyInfo.cs"
    Write-Host "Setting version in $assemblyInfoFile..."

    $fileVersion = "$($version.Split("-")[0])"

    $result = [System.IO.File]::ReadAllText($assemblyInfoFile)

    $assemblyFileVersionPattern = [regex]'\[assembly: AssemblyFileVersion\(".*"\)\]'
    $result = $assemblyFileVersionPattern.Replace($result, "[assembly: AssemblyFileVersion(""$fileVersion"")]")

    $assemblyInfoVersionPattern = [regex]'\[assembly: AssemblyInformationalVersion\(".*"\)\]'
    $result = $assemblyInfoVersionPattern.Replace($result, "[assembly: AssemblyInformationalVersion(""$version-$commit"")]")

    [System.IO.File]::WriteAllText($assemblyInfoFile, $result, [System.Text.Encoding]::UTF8)
}

function Get-Git-Commit-Short {
    return $(Get-Git-Commit).Substring(0, 7)
}

function Get-Git-Commit {
    if (Get-Command "git" -ErrorAction SilentlyContinue) {
        return & git rev-parse HEAD
    } else {
        return "0000000"
    }
}
