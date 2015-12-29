function Test-GetVaultProfiles {
	ipmo ..\..\..\ACMESharp.POSH\bin\Debug\ACMEPowerShell
	Get-ACMEVaultProfile -ListProfiles
}
