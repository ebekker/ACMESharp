param(
	[Parameter(Mandatory)]
	[string[]]$Targets,
	
	[switch]$Decrypt,
	[string]$Secret
)

$sfExec = "$PSScriptRoot\..\..\packages\secure-file.1.0.31\tools\secure-file.exe"

if (-not $Secret) {
	$cred = Get-Credential -Message "Specify the password to encrypt with (Username is ignored)." -UserName "IGNORED"
	$Secret = $cred.GetNetworkCredential().Password
}

if (-not $Secret) {
    Write-Warning "No Secret resolved.  Aborted."
}

foreach ($t in (Get-ChildItem $Targets)) {
	if ($Decrypt) {
		Write-Output "Decrypting [$t]"
		& $sfExec -decrypt $t -secret $Secret
	}
	else {
		Write-Output "Encrypting [$t]"
		& $sfExec -encrypt $t -secret $Secret
	}
}
