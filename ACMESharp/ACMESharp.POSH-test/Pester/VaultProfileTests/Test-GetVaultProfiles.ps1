function Test-GetVaultProfiles {
	ipmo $PSScriptRoot\..\..\bin\Debug\ACMEPowerShell
	Get-ACMEVaultProfile -ListProfiles
}
