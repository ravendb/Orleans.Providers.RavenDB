function CleanDir ($dir) {
    Write-Host "Cleaning $dir..."
    if (Test-Path -Path $dir) {
        Remove-Item -Recurse -Force $dir
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

function CleanSrcDirs ([string[]] $srcDirs) {
    foreach ($dir in $srcDirs) {
        CleanBinDir $dir
        CleanObjDir $dir
    }
}

function CleanBinDir ($srcDir) {
    $binDir = [io.path]::Combine($srcDir, "bin")
    CleanDir $binDir
}

function CleanObjDir ($srcDir) {
    $objDir = [io.path]::Combine($srcDir, "obj")
    CleanDir $objDir
}

function CleanFiles ($dir) {
    $files = Get-ChildItem -Path $dir -File
    foreach ($f in $files) { 
        Write-Host "Removing $($f.FullName)..."
        Remove-Item $f.FullName 
    }
}
