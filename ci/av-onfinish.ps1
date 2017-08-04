## Check if we should Enable RDP access
if ([int]$((Resolve-DnsName blockrdp.ci.acmesharp.bkkr.us -Type TXT).Text)) {
    Write-Warning "Detected RDP access request"
    ## As per:  https://www.appveyor.com/docs/how-to/rdp-to-build-worker/
    $blockRdp = $true
    iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/appveyor/ci/master/scripts/enable-rdp.ps1'))
}
else {
    Write-Output "No RDP access requested"
}
