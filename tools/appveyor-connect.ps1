
$ign = "$PSScriptRoot\_IGNORE"
md $ign -Force | Out-Null

$rdpTemplate   = "$PSScriptRoot\appveyor-connect.template.rdp"
$rdpLastServer = "$ign\lastServer.txt"
$rdpProfile    = "$ign\appveyor.rdp"

#$avDetails = @'
#  Server: 74.205.54.20:33931
#  Username: appveyor
#  Password: %KFGJzhKUlcD$jX
#'@
$avDetails = Get-Clipboard -TextFormatType Text
if (-not $avDetails -or -not "$avDetails".Trim().Length) {
    Write-Warning "Copy the connection details from AV console to the clipboard and TRY AGAIN!"
    return
}

$hostport   = ""
$username = ""
$password = ""

if ("$avDetails" -imatch "server:\s*([^\s]+)"  ) { $hostport = $Matches[1] }
if ("$avDetails" -imatch "username:\s*([^\s]+)") { $username = $Matches[1] }
if ("$avDetails" -imatch "password:\s*([^\s]+)") { $password = $Matches[1] }

Write-Output @"
Extracted Connection Details:
    Server:   $hostport
    Username: $username
    Password: $password
"@

if (-not $hostport -or -not $username -or -not $password) {
    Write-Warning "Unable to extract all the connection details"
    return
}

if (Test-Path -PathType Leaf $rdpLastServer) {
    $lastServer = [System.IO.File]::ReadAllText($rdpLastServer)
    Write-Warning "Detected LAST SERVER [$lastServer]"
    if ($lastServer) {
        Write-Warning "Deleting Credentials for LAST SERVER [$lastServer]"
        cmdkey /delete:`"$lastServer`"
    }
}

[System.IO.File]::WriteAllText($rdpLastServer, $hostport)

$rdpDetails = [System.IO.File]::ReadAllText($rdpTemplate)
$rdpDetails = $rdpDetails.Replace("@@SERVER@@", $hostport)
[System.IO.File]::WriteAllText($rdpProfile, $rdpDetails)


Write-Warning "Adding Credentials for [$hostport]"
cmdkey /generic:`"$hostport`" /user:`"$username`" /pass:`"$password`"

Write-Output "COPYING PASSWORD TO CLIPBOARD"
Set-Clipboard -Value $password

mstsc $rdpProfile
