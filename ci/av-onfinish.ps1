$doRdp = $false
try { $doRdp = ((wget http://acmesharp.zyborg.io/appveyor-rdp.txt).Content -eq 1) }
catch { }
if ($doRdp) {
  Write-Warning "Detected RDP access request"
  $blockRdp = $true
  iex ((new-object net.webclient).DownloadString(
	  'https://raw.githubusercontent.com/appveyor/ci/master/scripts/enable-rdp.ps1'))
}
else {
  Write-Output "No RDP access requested"
}
