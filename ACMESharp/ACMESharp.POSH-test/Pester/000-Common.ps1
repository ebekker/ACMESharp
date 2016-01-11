
Set-StrictMode -Version Latest

$TEST_DNS_ID    = "acme-pester.acmetesting.zyborg.io"
$TEST_MY_DNS_ID = "acme-pester-ps$($PSVersionTable.PSVersion.Major).acmetesting.zyborg.io"


if (-not (Get-Variable ACME_POSH_PATH -ValueOnly -ErrorAction Ignore)) {
    $ACME_POSH_PATH = "$PSScriptRoot\..\bin\ACMEPowerShell"
}
Write-Host "Resolve ACMESharp POSH Module path to [$ACME_POSH_PATH]"
ipmo $ACME_POSH_PATH


function Test-IsAdmin {
    ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
            [Security.Principal.WindowsBuiltInRole] "Administrator")
}

