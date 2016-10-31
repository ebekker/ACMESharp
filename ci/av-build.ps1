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

	Write-Output "Building choco packages"
	.\ACMESharp\ACMESharp.POSH\choco\acmesharp-posh\choco-pack.cmd
	.\ACMESharp\ACMESharp.POSH-test\choco\acmesharp-posh-all\choco-pack.cmd

    Write-Output "Publishing POSH modules to staging repo:"
    Import-Module PowerShellGet -Force
    Write-Output "  * Registering STAGING repo"
    Register-PSRepository -Name STAGING -PackageManagementProvider NuGet -InstallationPolicy Trusted `
            -SourceLocation https://int.nugettest.org/api/v2 `
            -PublishLocation https://int.nugettest.org/api/v2/package

    $modName = "ACMESharp.Providers.CloudFlare"
    ## First we need to publish the module which will force the packaging process of the PSGet module
    $modVer = "0.8.0.$($env:APPVEYOR_BUILD_NUMBER)"
    $modPsd1 = ".\ACMESharp\$($modName)\bin\$($env:CONFIGURATION)\$($modName)\$($modName).psd1"
    
    Write-Output "  * Updating Module Manifest Version [$modVer]"
    ## Unfortunately, this is not working, returning this error:
    ## Update-ModuleManifest : Cannot update the manifest properly. 'The specified RequiredModules entry 'ACMESharp' in the module manifest 'C:\projects\acmesharp\ACMESharp\ACMESharp.Providers.CloudFlare\bin\Debug\ACMESharp.Providers.CloudFlare\ACMESharp.Providers.CloudFlare.psd1' is invalid. Try again after updating this entry with valid values.'
    ##  BUT, only on the AV Build server!!! Which is PS5.1, not on PS5.0
    #Update-ModuleManifest -Path ".\ACMESharp\$($modName)\bin\$($env:CONFIGURATION)\$($modName)\$($modName).psd1" `
    #        -ModuleVersion $modVer
    ## Instead we do a simply find and replace of the version string
    $modPsd1Body = Get-Content -Path $modPsd1
    $modPsd1Body = $modPsd1Body -replace '(ModuleVersion[^\d]+\d+\.\d+\.\d+)(\.\d+)?',"`$1.$($env:APPVEYOR_BUILD_NUMBER)"
    Set-Content -Path $modPsd1 -Value $modPsd1Body

    Write-Output "  * Publishing CloudFlare Provider module [$modName]"
    Publish-Module -Path ".\ACMESharp\$($modName)\bin\$($env:CONFIGURATION)\$($modName)" `
            -Repository STAGING -NuGetApiKey $env:STAGING_NUGET_APIKEY -Force -ErrorAction Stop

    ## Then we pull the module back down from the STAGING repo 
    #$modPkgWeb = Invoke-WebRequest -Uri "https://staging.nuget.org/api/v2/package/$($modName)" -MaximumRedirection 0 -ErrorAction Ignore
    #$modPkgUri = New-Object uri($modPkgWeb.Headers.Location)
    #$modPkg = $modPkgUri.Segments[-1]
    Invoke-WebRequest -Uri "https://staging.nuget.org/api/v2/package/$($modName)/$($modVer)" `
            -OutFile ".\ACMESharp\$($modName)\bin\$($modName).$($modVer).nupkg"
}
