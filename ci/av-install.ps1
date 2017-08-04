## Check if we should Enable RDP access
if ([int]$((Resolve-DnsName blockrdp.ci.acmesharp.bkkr.us -Type TXT).Text)) {
    Write-Warning "Detected RDP access request"
    ## As per:  https://www.appveyor.com/docs/how-to/rdp-to-build-worker/
    iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/appveyor/ci/master/scripts/enable-rdp.ps1'))
}
else {
    Write-Output "No RDP access requested"
}

Write-Output "RESTORING for solution file"
nuget restore ACMESharp\ACMESharp.sln
Write-Output "Installing 'secure-file' NuGet package"
nuget install secure-file -ExcludeVersion

Write-Output "Unlocking params for general unit tests"
secure-file\tools\secure-file -secret $env:secureInfoPassword -decrypt ACMESharp\ACMESharp-test\config\dnsInfo.json.enc
secure-file\tools\secure-file -secret $env:secureInfoPassword -decrypt ACMESharp\ACMESharp-test\config\webServerInfo.json.enc
secure-file\tools\secure-file -secret $env:secureInfoPassword -decrypt ACMESharp\ACMESharp-test\config\testProxyConfig.json.enc

Write-Output "Unlocking params for Providers unit tests"
& .\ACMESharp\ACMESharp.Providers-test\Config\secure-AllParamsJson.ps1 -Decrypt -Secret $env:secureInfoPassword

## Enable this to debug what's on the build host
#Write-Output "Installed Software:"
#$x64items = @(Get-ChildItem "HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")
#$x64items + @(Get-ChildItem "HKLM:SOFTWARE\wow6432node\Microsoft\Windows\CurrentVersion\Uninstall") `
#   | ForEach-object { Get-ItemProperty Microsoft.PowerShell.Core\Registry::$_ } `
#   | Sort-Object -Property DisplayName `
#   | Select-Object -Property DisplayName,DisplayVersion
#$x64items

## Enable this to debug what's on the build host
#Write-Output "PowerShell Versions:"
#Write-Output ($PSVersionTable | ConvertTo-Json)

## Need to install NuGet for Publish-Module to work as per:
##   http://help.appveyor.com/discussions/problems/3469-psgetpsm1-doesnt-work
Write-Output "Installing NUGET PackageProvider"
Get-PackageProvider -Name NuGet -Force

Write-Output "Updating to the latest PSGet module"

## Enable this to debug what's on the build host
#Write-Output "  * Mod Info BEFORE install:"
#Write-Output (Get-Module PowerShellGet -ListAvailable | ConvertTo-Json -Depth 2)

Install-Module -Name PowerShellGet -Force

## Enable this to debug what's on the build host
#Write-Output "  * Mod Info AFTER install:"
#Write-Output (Get-Module PowerShellGet -ListAvailable | ConvertTo-Json -Depth 2)
