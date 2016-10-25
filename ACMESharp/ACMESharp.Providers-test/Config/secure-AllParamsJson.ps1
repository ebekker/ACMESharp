param(
	[switch]$Decrypt,
	[string]$Secret
)

if ($Decrypt) {
	& "$PSScriptRoot\secure-files.ps1" -Secret $Secret -Targets "*Params.json.enc" -Decrypt
}
else {
	& "$PSScriptRoot\secure-files.ps1" -Secret $Secret -Targets "*Params.json"
}
