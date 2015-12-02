$doRdp = $false
try { $doRdp = ((wget http://acmesharp.zyborg.io/appveyor-rdp.txt).Content -eq 1) }
catch { }
if ($doRdp) {
  Write-Warning "Detected RDP access request"
  iex ((new-object net.webclient).DownloadString(
	  'https://raw.githubusercontent.com/appveyor/ci/master/scripts/enable-rdp.ps1'))
}
else {
  Write-Output "No RDP access requested"
}

nuget restore ACMESharp\ACMESharp.sln
nuget install secure-file -ExcludeVersion
secure-file\tools\secure-file -secret $env:secureInfoPassword -decrypt ACMESharp\ACMESharp-test\config\dnsInfo.json.enc
secure-file\tools\secure-file -secret $env:secureInfoPassword -decrypt ACMESharp\ACMESharp-test\config\webServerInfo.json.enc
secure-file\tools\secure-file -secret $env:secureInfoPassword -decrypt ACMESharp\ACMESharp-test\config\testProxyConfig.json.enc
