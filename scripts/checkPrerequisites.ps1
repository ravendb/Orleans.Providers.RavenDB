function CheckPrerequisites () {
    if ($null -eq (Get-Command "git" -ErrorAction SilentlyContinue)) {
        throw "git not found in path."
    }

    if ($null -eq (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
        throw "dotnet not found in path."
    }

    if ($PSVersionTable.PSVersion.Major -lt 5) {
        throw "Incompatible PowerShell version. Must be 5 or later."
    }
}
