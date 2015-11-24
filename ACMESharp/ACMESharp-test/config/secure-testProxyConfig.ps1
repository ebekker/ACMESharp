
$sfExec = "$PSScriptRoot\..\..\packages\secure-file.1.0.31\tools\secure-file.exe"
$target = "$PSScriptRoot\testProxyConfig.json"

$cred = Get-Credential -Message "Specify the password to encrypt with (Username is ignored)." -UserName "IGNORED"

if (-not $cred) {
    Write-Warning "Aborted."
}

& $sfExec -encrypt $target -secret $cred.GetNetworkCredential().Password
