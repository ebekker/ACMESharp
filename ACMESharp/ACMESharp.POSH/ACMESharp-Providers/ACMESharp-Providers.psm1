$ErrorActionPreference = 'Stop'

<#
.PARAMETER ModuleName
Required, the name of the PowerShell module that is an ACMESharp Extension Module.

.PARAMETER ModuleVersion
An optional version spec, useful if multiple version of the target Extension Module
are installed.

The spec can be an exact version string or a `-like` pattern to be matched.

.PARAMETER AcmeVersion
An optional version spec, useful if multiple versions of the core ACMESharp module is installed,
this will specify which module installation will be targeted for enabling the module.

The spec can be an exact version string or a `-like` pattern to be matched.
#>
function Resolve-ProviderModule {
	param(
		[Parameter(Mandatory)]
		[string]$ModuleName,
		[Parameter(Mandatory=$false)]
		[string]$ModuleVersion,

		[Parameter(Mandatory=$false)]
		[string]$AcmeVersion
	)

	## Get any modules that are resident in the current session and
	## any module versions that are available on the current system
	$acmeMods = @(Get-Module ACMESharp) + @(Get-Module -ListAvailable ACMESharp | sort -Descending Version)
	$provMods = @(Get-Module $ModuleName) + (Get-Module -ListAvailable $ModuleName | sort -Descending Version)

	if ($AcmeVersion) {
		$acmeMod = $acmeMods | ? { $_.Version -like $AcmeVersion } | select -First 1
	}
	else {
		$acmeMod = $acmeMods | select -First 1
	}

	if ($ModuleVersion) {
		$provMod = $provMods | ? { $_.Version -like $ModuleVersion } | select -First 1
	}
	else {
		$provMod = $provMods | select -First 1
	}

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

	$extRoot = "$($acmeMod.ModuleBase)\EXT"
	$extPath = "$($extRoot)\$($provMod.Name).extlnk"

	[ordered]@{
		acmeMod = $acmeMod
		provMod = $provMod
		extRoot = $extRoot
		extPath = $extPath
	}		
}

<#
.PARAMETER ModuleName
Required, the name of the PowerShell module that is an ACMESharp Extension Module.

.PARAMETER ModuleVersion
An optional version spec, useful if multiple version of the target Extension Module
are installed.

The spec can be an exact version string or a `-like` pattern to be matched.

.PARAMETER AcmeVersion
An optional version spec, useful if multiple versions of the core ACMESharp module is installed,
this will specify which module installation will be targeted for enabling the module.

The spec can be an exact version string or a `-like` pattern to be matched.
#>
function Enable-ProviderModule {
	param(
		[Parameter(Mandatory)]
		[string]$ModuleName,
		[Parameter(Mandatory=$false)]
		[string]$ModuleVersion,

		[Parameter(Mandatory=$false)]
		[string]$AcmeVersion
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
	@{
		Path = $deps.provMod.ModuleBase
		Version = $deps.provMod.Version.ToString()
	} | ConvertTo-Json > $deps.extPath
}

<#
.PARAMETER ModuleName
Required, the name of the PowerShell module that is an ACMESharp Extension Module.

.PARAMETER ModuleVersion
An optional version spec, useful if multiple version of the target Extension Module
are installed.

The spec can be an exact version string or a `-like` pattern to be matched.

.PARAMETER AcmeVersion
An optional version spec, useful if multiple versions of the core ACMESharp module is installed,
this will specify which module installation will be targeted for enabling the module.

The spec can be an exact version string or a `-like` pattern to be matched.
#>
function Disable-ProviderModule {
	param(
		[Parameter(Mandatory)]
		[string]$ModuleName,
		[Parameter(Mandatory=$false)]
		[string]$ModuleVersion,

		[Parameter(Mandatory=$false)]
		[string]$AcmeVersion
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
