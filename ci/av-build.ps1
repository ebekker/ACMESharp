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
	## References:
	##    https://scan.coverity.com/download?tab=csharp
	##    https://github.com/appveyor/ci/issues/144
	##    https://github.com/OpenRA/OpenRA/pull/8313/files

	Write-Warning "Detected build with Coverity Scan request"
	& cov-build.exe --dir cov-int $msb_prog $msb_args
	& nuget.exe install PublishCoverity -ExcludeVersion
	& PublishCoverity\tools\PublishCoverity.exe compress -o coverity.zip -i cov-int
	$covDateTime = Get-Date -format s
	& PublishCoverity\tools\PublishCoverity.exe publish `
			-t "$env:COVERITY_PROJECT_TOKEN" `
			-e "$env:COVERITY_NOTIFICATION_EMAIL" `
			-r "$env:APPVEYOR_REPO_NAME" `
			-z coverity.zip `
			-d "AppVeyor Coverity build ($env:APPVEYOR_BUILD_VERSION @ $covDateTime)." `
			--codeVersion $env:APPVEYOR_BUILD_VERSION
}
else {
    Write-Output "Running *normal* build (i.e. no Coverity)"
    & $msb_prog $msb_args

    Write-Output "Building nuget packages"
	.\ACMESharp\nuget-build.cmd ACMESharp
	.\ACMESharp\nuget-build.cmd ACMESharp.PKI.Providers.BouncyCastle
	.\ACMESharp\nuget-build.cmd ACMESharp.PKI.Providers.OpenSslLib32
	.\ACMESharp\nuget-build.cmd ACMESharp.PKI.Providers.OpenSslLib64

	.\ACMESharp\nuget-build.cmd ACMESharp.Vault
	.\ACMESharp\nuget-build.cmd ACMESharp.Providers.IIS
	.\ACMESharp\nuget-build.cmd ACMESharp.Providers.Windows
	.\ACMESharp\nuget-build.cmd ACMESharp.Providers.AWS
	.\ACMESharp\nuget-build.cmd ACMESharp.Providers.CloudFlare
	.\ACMESharp\nuget-build.cmd ACMESharp.POSH -Exclude README-TESTING.txt -Exclude en-us\*

	Write-Output "Building choco packages"
	.\ACMESharp\ACMESharp.POSH\choco\acmesharp-posh\choco-pack.cmd
	.\ACMESharp\ACMESharp.POSH-test\choco\acmesharp-posh-all\choco-pack.cmd

    Write-Output "Publishing POSH modules to staging repo:"
    Import-Module PowerShellGet -Force
    Write-Output "  * Registering STAGING repo"

	## NuGet Staging
    #$PSGalleryPublishUri = 'https://int.nugettest.org/api/v2/package'
    #$PSGallerySourceUri  = 'https://int.nugettest.org/api/v2'
	#$PSGalleryApiKey     = $env:STAGING_NUGET_APIKEY

	## MyGet - Our Own ACMESharp-POSH Staging
	$PSGalleryPublishUri = 'https://www.myget.org/F/acmesharp-posh-staging/api/v2/package'
	$PSGallerySourceUri  = 'https://www.myget.org/F/acmesharp-posh-staging/api/v2'
	$PSGalleryApiKey     = $env:STAGING_MYGET_APIKEY

    Register-PSRepository -Name STAGING -PackageManagementProvider NuGet -InstallationPolicy Trusted `
            -PublishLocation $PSGalleryPublishUri `
            -SourceLocation $PSGallerySourceUri


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
    #!        -NuGetApiKey $PSGalleryApiKey -Force -ErrorAction Stop
	#!
    #!## Then we pull the module back down from the STAGING repo 
    #!Invoke-WebRequest -Uri "$($PSGalleryPublishUri)/$($modName)/$($modVer)" `
    #!        -OutFile ".\ACMESharp\$($modName)\bin\$($env:CONFIGURATION)\$($modName).$($modVer).nupkg"
    #!
	#!
    #!## We need to update the PSModPath so that we can resolve the "RequiredModules"
    #!## dependency to ACMESharp in the upcoming Module Manifests
    #!$acmeModPath = (Resolve-Path $modPath).Path
    #!$env:PSModulePath += ";$acmeModPath"

	## The AWS Provider down below needs this to be in
	## the STAGING repo in order to pass validations.
	## This will normally already be in the target repo due
	## due to prior publications, that's why we ignore on error
	## but this is just in case the module was "wiped out" from
	## a staging repo since there are no guarantees they exist
	Write-Output "Installing Fake AWSPowerShell Module to resolve dependencies"
	try {
		Publish-Module -Repository staging -Path .\ci\nuget-staging\AWSPowerShell `
				-NuGetApiKey $PSGalleryApiKey -Force
	}
	catch [System.InvalidOperationException] {
		## Testing for something like this:
		##    The module 'AWSPowerShell' with version '0.0.1.0' cannot be published as the current version '0.0.1.0' is already available in the repository 
		$errMsg = $Error[0].Exception.Message
		if ($errMsg -and
				($errMsg -match 'cannot be published') -and
				($errMsg -match 'is already available')) {
			Write-Warning "Looks like AWSPowerShell is already published; should be safe to ignore"
		}
		else {
			## Otherwise re-throw it
			throw $Error[0].Exception
		}
	}


	$poshModules = [ordered]@{
		## Embedded '@' overrides the default Project Folder
		"ACMESharp@ACMESharp.POSH"       = $env:APPVEYOR_BUILD_VERSION
		"ACMESharp.Providers.IIS"        = "0.8.0.$($env:APPVEYOR_BUILD_NUMBER)"
		"ACMESharp.Providers.AWS"        = "0.8.0.$($env:APPVEYOR_BUILD_NUMBER)"
		"ACMESharp.Providers.CloudFlare" = "0.8.0.$($env:APPVEYOR_BUILD_NUMBER)"
		"ACMESharp.Providers.Windows"    = "0.8.0.$($env:APPVEYOR_BUILD_NUMBER)"
	}

	foreach ($modName in $poshModules.Keys) {
		#$modName = "ACMESharp.Providers.CloudFlare"
		$modVer = $poshModules[$modName] ## "0.8.0.$($env:APPVEYOR_BUILD_NUMBER)"
		$modDir = $modName
		if ($modName -match '@') {
			($modName, $modDir) = $modName -split '@'
		}

		Write-Output "Publishing to STAGING NuGet [$modName]"
		Write-Output "  from project folder [$modDir]"
		## First we need to publish the module which will force the packaging process of the PSGet module
		$modPath = ".\ACMESharp\$($modDir)\bin\$($env:CONFIGURATION)\$($modName)"
		$modPsd1 = "$($modPath)\$($modName).psd1"

		Write-Output "  * Updating Module Manifest [$modPsd1] to Version [$modVer]"
		Update-ModuleManifest -Path $modPsd1 -ModuleVersion $modVer

		Write-Output "  * Publishing module [$modName]"
		Publish-Module -Path $modPath -Repository STAGING `
				-NuGetApiKey $PSGalleryApiKey -Force -ErrorAction Stop

		## Then we pull the module back down from the STAGING repo 
		#$modPkgWeb = Invoke-WebRequest -Uri "$($PSGalleryPublishUri)/$($modName)" -MaximumRedirection 0 -ErrorAction Ignore
		#$modPkgUri = New-Object uri($modPkgWeb.Headers.Location)
		#$modPkg = $modPkgUri.Segments[-1]

		$modPoshDir = ".\ACMESharp\$($modDir)\bin\posh"
		$modPoshPkg = "$($modPoshDir)\$($modName).$($modVer).nupkg"
		mkdir -Force $modPoshDir
		Invoke-WebRequest -Uri "$($PSGalleryPublishUri)/$($modName)/$($modVer)" `
				-OutFile $modPoshPkg
		#		-OutFile ".\ACMESharp\$($modName)\bin\$($env:CONFIGURATION)\$($modName).$($modVer).nupkg"


		## We update the PSModPath with each module we process in case any
		## subsequent modules require the preceding modules a a dependency,
		## for example to resolve the "RequiredModules" dependency, such as
		## references to the base ACMESharp module
		$acmeModPath = (Resolve-Path $modPath).Path
		$env:PSModulePath += ";$acmeModPath"
	}
}
