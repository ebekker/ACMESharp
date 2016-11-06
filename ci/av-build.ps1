## This was copied from a build session with custom build command disabled
##    msbuild "ACMESharp\ACMESharp.sln" /verbosity:minimal /logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"

## This logic was adapted from:
##    http://arkfps.github.io/2015/01/07/using-coverity-scan-with-appveyor/

$msb_prog = "msbuild"
$msb_args = @(
    ,'ACMESharp\ACMESharp.sln'
	,'/verbosity:minimal'
    ,'/logger:C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll'
	
	## These can be set to target specific build target configuration
	#,'/p:Configuration=$env:CONFIGURATION'
	#,'/p:Platform=$env:PLATFORM'
)

$doCoverity = $false
try { $doCoverity = ((wget http://acmesharp.zyborg.io/appveyor-coverity.txt).Content -eq 1) }
catch { }
if ($doCoverity) {
    Write-Warning "Detected build with Coverity Scan request"
	& cov-build.exe --dir cov-int $msb_prog $msb_args
}
else {
    Write-Output "Running *normal* build"
    & $msb_prog $msb_args

    Write-Output "Building nuget packages"
	.\ACMESharp\ACMESharp\mynuget.cmd
	.\ACMESharp\ACMESharp.PKI.Providers.OpenSslLib32\mynuget.cmd
	.\ACMESharp\ACMESharp.PKI.Providers.OpenSslLib64\mynuget.cmd

	.\ACMESharp\nuget-build.cmd ACMESharp.Vault
	.\ACMESharp\nuget-build.cmd ACMESharp.Providers.IIS
	.\ACMESharp\nuget-build.cmd ACMESharp.Providers.AWS
	.\ACMESharp\nuget-build.cmd ACMESharp.Providers.CloudFlare

	Write-Output "Building choco packages"
	.\ACMESharp\ACMESharp.POSH\choco\acmesharp-posh\choco-pack.cmd
	.\ACMESharp\ACMESharp.POSH-test\choco\acmesharp-posh-all\choco-pack.cmd

    Write-Output "Publishing POSH modules to staging repo:"
    Import-Module PowerShellGet -Force
    Write-Output "  * Registering STAGING repo"
    Register-PSRepository -Name STAGING -PackageManagementProvider NuGet -InstallationPolicy Trusted `
            -SourceLocation https://int.nugettest.org/api/v2 `
            -PublishLocation https://int.nugettest.org/api/v2/package


    #!$modName = "ACMESharp"
    #!Write-Output "Publishing to STAGING NuGet [$modName]"
    #!$modVer = $env:APPVEYOR_BUILD_VERSION
    #!$modPath = ".\ACMESharp\$($modName).POSH\bin\$($env:CONFIGURATION)\$($modName)"
    #!$modPsd1 = "$($modPath)\$($modName).psd1"
	#!
    #!Write-Output "  * Updating Module Manifest Version [$modVer]"
    #!Update-ModuleManifest -Path $modPsd1 -ModuleVersion $modVer
	#!
    #!Write-Output "  * Publishing ACMESharp main module [$modName]"
    #!Publish-Module -Path $modPath -Repository STAGING `
    #!        -NuGetApiKey $env:STAGING_NUGET_APIKEY -Force -ErrorAction Stop
	#!
    #!## Then we pull the module back down from the STAGING repo 
    #!Invoke-WebRequest -Uri "https://staging.nuget.org/api/v2/package/$($modName)/$($modVer)" `
    #!        -OutFile ".\ACMESharp\$($modName)\bin\$($env:CONFIGURATION)\$($modName).$($modVer).nupkg"
    #!
	#!
    #!## We need to update the PSModPath so that we can resolve the "RequiredModules"
    #!## dependency to ACMESharp in the upcoming Module Manifests
    #!$acmeModPath = (Resolve-Path $modPath).Path
    #!$env:PSModulePath += ";$acmeModPath"

	$poshModules = [ordered]@{
		## Embedded '@' overrides the default Project Folder
		"ACMESharp@ACMESharp.POSH"       = $env:APPVEYOR_BUILD_VERSION
		"ACMESharp.Providers.IIS"        = "0.8.0.$($env:APPVEYOR_BUILD_NUMBER)"
		"ACMESharp.Providers.AWS"        = "0.8.0.$($env:APPVEYOR_BUILD_NUMBER)"
		"ACMESharp.Providers.CloudFlare" = "0.8.0.$($env:APPVEYOR_BUILD_NUMBER)"
	}

	foreach ($modName in $poshModules.Keys) {
		#$modName = "ACMESharp.Providers.CloudFlare"
		$modVer = $poshModules[$modName] ## "0.8.0.$($env:APPVEYOR_BUILD_NUMBER)"
		$modDir = $modName
		if ($modName -match '@') {
			($modName, $modDir) = $modName -split '@'
		}

		Write-Output "Publishing to STAGING NuGet [$modName]"
		Write-Output "    from project folder [$modDir]"
		## First we need to publish the module which will force the packaging process of the PSGet module
		$modPath = ".\ACMESharp\$($modDir)\bin\$($env:CONFIGURATION)\$($modName)"
		$modPsd1 = "$($modPath)\$($modName).psd1"

		Write-Output "  * Updating Module Manifest Version [$modVer]"
		Update-ModuleManifest -Path $modPsd1 -ModuleVersion $modVer

		Write-Output "  * Publishing CloudFlare Provider module [$modName]"
		Publish-Module -Path $modPath -Repository STAGING `
				-NuGetApiKey $env:STAGING_NUGET_APIKEY -Force -ErrorAction Stop

		## Then we pull the module back down from the STAGING repo 
		#$modPkgWeb = Invoke-WebRequest -Uri "https://staging.nuget.org/api/v2/package/$($modName)" -MaximumRedirection 0 -ErrorAction Ignore
		#$modPkgUri = New-Object uri($modPkgWeb.Headers.Location)
		#$modPkg = $modPkgUri.Segments[-1]
		Invoke-WebRequest -Uri "https://staging.nuget.org/api/v2/package/$($modName)/$($modVer)" `
				-OutFile ".\ACMESharp\$($modName)\bin\$($env:CONFIGURATION)\$($modName).$($modVer).nupkg"


		## We update the PSModPath with each module we process in case any
		## subsequent modules require the preceding modules a a dependency,
		## for example to resolve the "RequiredModules" dependency, such as
		## references to the base ACMESharp module
		$acmeModPath = (Resolve-Path $modPath).Path
		$env:PSModulePath += ";$acmeModPath"
	}
}
