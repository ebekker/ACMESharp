param(
	[switch]$Decrypt,
	[string]$Secret
)

if ($Decrypt) {
	& "$PSScriptRoot\secure-files.ps1" -Targets "*Params.json.enc" -Decrypt
}
else {
	& "$PSScriptRoot\secure-files.ps1" -Targets "*Params.json"
}
