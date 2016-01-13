
$ErrorActionPreference = 'Stop'; # stop on all errors

$packageName = 'acmesharp-posh-all'
$poshModuleName = 'ACMESharp'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$isAdmin = Test-ProcessAdminRights

$localInstallPathSave = "$($env:LOCALAPPDATA)\acmesharp-installpath.txt"
$globalInstallPathSave = "$toolsDir\acmesharp-installpath.txt"

if (-not (Test-Path $localInstallPathSave -PathType Leaf) -and
        -not (Test-Path $globalInstallPathSave -PathType Leaf)) {
    Write-Warning "Could not find module in either LOCAL or GLOBAL modules path"
    return
}

if (Test-Path $localInstallPathSave) {
    $installPath = [System.IO.File]::ReadAllText($localInstallPathSave)
    if (Test-Path $installPath) {
        Write-Warning "Removing LOCAL PS Module directory at [$installPath]"
        rd $installPath -Recurse
        del $localInstallPathSave
    }
    else {
        Write-Warning "PS Module was expected at LOCAL [$installPath] but was not found"
    }
}

if (Test-Path $globalInstallPathSave) {
    $installPath = [System.IO.File]::ReadAllText($globalInstallPathSave)
    if (Test-Path $installPath) {
        Write-Warning "Removing GLOBAL PS Module directory at [$installPath]"

        if (-not $isAdmin) {
            Write-Warning "Attempting to remove from GLOBAL as non-Admin; this may fail..."
        }

        rd $installPath -Recurse
        del $globalInstallPathSave
    }
    else {
        Write-Warning "PS Module was expected at GLOBAL [$installPath] but was not found"
    }
}
