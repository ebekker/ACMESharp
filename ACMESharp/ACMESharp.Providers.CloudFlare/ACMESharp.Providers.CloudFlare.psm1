$PROVIDER_NAME = "CloudFlare"
$MODULE_NAME = "ACMESharp.Providers.CloudFlare"

$ErrorActionPreference = 'Stop'

Import-Module ACMESharp

function Resolve-CloudFlareProvider {
	$acmeMod = Get-Module ACMESharp
	$thisMod = Get-Module $MODULE_NAME

	if (-not $thisMod -or -not $thisMod.ModuleBase) {
		Write-Error "Cannot resolve this module's own base"
		return
	}
	if (-not $acmeMod -or -not $acmeMod.ModuleBase) {
		Write-Error "$PROVIDER_NAME Provider requires ACMESharp module"
		return
	}
	if (-not (Test-Path $acmeMod.ModuleBase)) {
		Write-Error "Cannot find ACMESharp module base [$($x.ModuleBase)]"
		return
	}

	$extRoot = "$($acmeMod.ModuleBase)\Ext"
	$extPath = "$($extRoot)\$($MODULE_NAME)"

	@{
		acmeMod = $acmeMod
		thisMod = $thisMod
		extRoot = $extRoot
		extPath = $extPath
	}		
}

function Enable-CloudFlareProvider {
	
	$deps = Resolve-CloudFlareProvider
	if (-not $deps) {
		return
	}

	if (Test-Path $deps.extPath) {
		Write-Error "$PROVIDER_NAME Provider Extension already enabled"
		return
	}
	mkdir -Force $deps.extPath
	Write-Output "Installing $PROVIDER_NAME Provider Extension to directory [$($deps.extPath)]"
	copy -Recurse "$($deps.thisMod.ModuleBase)\*" $deps.extPath -Force
}

function Disable-CloudFlareProvider {
	$deps = Resolve-CloudFlareProvider
	if (-not $deps) {
		return
	}

	if (-not (Test-Path $deps.extPath)) {
		Write-Error "$PROVIDER_NAME Provider Extension is not enabled"
		return
	}
	Write-Output "Removing $PROVIDER_NAME Provider Extension installed at directory [$($deps.extPath)]"
	rmdir -Confirm $deps.extPath
}
