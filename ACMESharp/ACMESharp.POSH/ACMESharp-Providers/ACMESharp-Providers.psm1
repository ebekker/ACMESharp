$ErrorActionPreference = 'Stop'

function Resolve-ProviderModule {
	param(
		[Parameter(Mandatory)]
		[string]$ModuleName
	)

	$acmeMod = Get-Module ACMESharp
	$provMod = Get-Module $ModuleName

	if (-not $provMod -or -not $provMod.ModuleBase) {
		Write-Error "Cannot resolve provider module's base [$ModuleName]"
		return
	}
	if (-not $acmeMod -or -not $acmeMod.ModuleBase) {
		Write-Error "Cannot resolve ACMESharp module's own base"
		return
	}
	if (-not (Test-Path $acmeMod.ModuleBase)) {
		Write-Error "Cannot find ACMESharp module base [$($x.ModuleBase)]"
		return
	}

	$extRoot = "$($acmeMod.ModuleBase)\Ext"
	$extPath = "$($extRoot)\$($ModuleName).extlnk"

	[ordered]@{
		acmeMod = $acmeMod
		provMod = $provMod
		extRoot = $extRoot
		extPath = $extPath
	}		
}

function Enable-ProviderModule {
	param(
		[Parameter(Mandatory)]
		[string]$ModuleName
	)
	
	$deps = Resolve-ProviderModule -ModuleName $ModuleName
	if (-not $deps) {
		return
	}

	if (Test-Path $deps.extPath) {
		Write-Error "Provider Extension already enabled"
		return
	}
	mkdir -Force $deps.extRoot
	Write-Output "Installing Provider Extension Module to [$($deps.extPath)]"
	@{ Path = $deps.provMod.ModuleBase } | ConvertTo-Json -Compress > $deps.extPath
}

function Disable-ProviderModule {
	param(
		[Parameter(Mandatory)]
		[string]$ModuleName
	)
	
	$deps = Resolve-ProviderModule -ModuleName $ModuleName
	if (-not $deps) {
		return
	}

	if (-not (Test-Path $deps.extPath)) {
		Write-Error "Provider Extension is not enabled"
		return
	}
	Write-Output "Removing Provider Extension Module installed at [$($deps.extPath)]"
	del -Confirm $deps.extPath
}
