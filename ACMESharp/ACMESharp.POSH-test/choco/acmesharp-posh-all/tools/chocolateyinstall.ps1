
$ErrorActionPreference = 'Stop'; # stop on all errors

$packageName = 'acmesharp-posh-all' # arbitrary name for the package, used in messages
$poshModuleName = 'ACMESharp'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$sourceDir = "$(Split-Path -parent $toolsDir)\source"

$isAdmin = Test-ProcessAdminRights
$psCurVer = $PSVersionTable.PSVersion.Major
$psMinVer = 3

if ($psCurVer -lt $psMinVer) {
    throw "Minimum PS version required is $psMinVer; $current PS version detected is $psCurVer"
}

$localInstallPathSave = "$($env:LOCALAPPDATA)\acmesharp-installpath.txt"
$globalInstallPathSave = "$toolsDir\acmesharp-installpath.txt"

$installPathSave = if ($isAdmin) { $globalInstallPathSave } else { $localInstallPathSave }

ipmo PsGet

$installModuleParams = @{
	Global     = $isAdmin
	ModulePath = "$sourceDir\$poshModuleName"
}

Install-Module @installModuleParams

## Once we install the module, try to load it and see where it
## is installed to so that we can remember for uninstallation
$modInfo = Get-Module $poshModuleName
if (-not $modInfo) {
    ipmo $poshModuleName
    $modInfo = Get-Module $poshModuleName
}
if (-not $modInfo) {
    Write-Warning "Unable to resolve the [$poshModuleName] module details"
}
else {
    $installPath = $modInfo.ModuleBase
    if ($installPath) {
        [System.IO.File]::WriteAllText($installPathSave, $installPath)
    }
}
