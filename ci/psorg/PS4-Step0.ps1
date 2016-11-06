## We need to make sure IIS is installed to test the IIS providers
if ($PSVersionTable.PSVersion.Major -ge 4) {
	## Works for PS 4 & 5
	Install-WindowsFeature -Name Web-Server -IncludeAllSubFeature -IncludeManagementTools
}
else {
	## Works for PS 3
	Add-WindowsFeature -Name Web-Server -IncludeAllSubFeature
}

function Get-MyIP {
	$ipJson = Invoke-WebRequest -Uri https://api.ipify.org?format=json -UseBasicParsing | ConvertFrom-Json
	$ipJson.ip
}
$x = Get-MyIP
Write-Host "My IP is $x"
$x