
$ErrorActionPreference = 'Stop'; # stop on all errors

$packageName = 'acmesharp-posh'
$poshModuleName = 'ACMESharp'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$isAdmin = Test-ProcessAdminRights

$localInstallPathSave = "$($env:LOCALAPPDATA)\acmesharp-installpath.txt"
$globalInstallPathSave = "$toolsDir\acmesharp-installpath.txt"

$installPathSave = if ($isAdmin) { $globalInstallPathSave } else { $localInstallPathSave }

if (Test-Path $installPathSave) {
    $installPath = [System.IO.File]::ReadAllText($installPathSave)
}
else {
    throw "Unable to resolve module installation path"
}

if (Test-Path $installPath) {
    Write-Warning "Removing PS Module directory at [$installPath]"
    rd $installPath -Recurse
    del $installPathSave
}
