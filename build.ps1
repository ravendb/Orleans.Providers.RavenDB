param(
  $Target="",
  [switch]$DryRunSign = $false
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Import helper scripts
. '.\scripts\checkLastExitCode.ps1'
. '.\scripts\checkPrerequisites.ps1'
. '.\scripts\clean.ps1'
. '.\scripts\getScriptDirectory.ps1'
. '.\scripts\version.ps1'
. '.\scripts\nuget.ps1'
. '.\scripts\updateSourceWithBuildInfo.ps1'
. '.\scripts\sign.ps1'

# Check prerequisites
CheckPrerequisites

# Define project paths
$PROJECT_DIR = Get-ScriptDirectory
$RELEASE_DIR = [io.path]::Combine($PROJECT_DIR, "artifacts")

$ORLEANS_SRC_DIR = [io.path]::Combine($PROJECT_DIR, "src", "Orleans.Providers.RavenDb")
$ORLEANS_OUT_DIR = [io.path]::Combine($ORLEANS_SRC_DIR, "bin", "Release")
$ORLEANS_DLL_PATH = [io.path]::Combine($ORLEANS_OUT_DIR, "net8.0", "Orleans.Providers.RavenDb.dll")

# Ensure artifacts directory exists
New-Item -Path $RELEASE_DIR -Type Directory -Force

# Clean previous build artifacts
CleanFiles $RELEASE_DIR
CleanSrcDirs $ORLEANS_SRC_DIR

# Generate version info
$versionObj = SetVersionInfo -projectDir $PROJECT_DIR
$version = $versionObj.Version
Write-Host -ForegroundColor Green "Building version: $version"

# Update source code with version information
UpdateSourceWithBuildInfo $PROJECT_DIR $version

# Build the project
BuildProject $ORLEANS_SRC_DIR

# Sign the built DLL (if needed)
SignFile $PROJECT_DIR $ORLEANS_DLL_PATH $DryRunSign

# Create NuGet package
CreateNugetPackage $ORLEANS_SRC_DIR $RELEASE_DIR $version

Write-Host "Done creating packages."
