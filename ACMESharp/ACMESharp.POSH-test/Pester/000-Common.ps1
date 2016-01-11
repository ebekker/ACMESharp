
Set-StrictMode -Version Latest

$TEST_DNS_ID       = "acme-pester.acmetesting.zyborg.io"
$TEST_MYIP_DNS_ID  = "acme-pester-ps$($PSVersionTable.PSVersion.Major)-myip.acmetesting.zyborg.io"
$TEST_DNS_CHLNG_ID = "acme-pester-ps$($PSVersionTable.PSVersion.Major)-chlng.acmetesting.zyborg.io"


if (-not (Get-Variable ACME_POSH_PATH -ValueOnly -ErrorAction Ignore)) {
    $ACME_POSH_PATH = "$PSScriptRoot\..\bin\ACMEPowerShell"
}
Write-Host "Resolve ACMESharp POSH Module path to [$ACME_POSH_PATH]"
ipmo $ACME_POSH_PATH


function Test-IsAdmin {
    ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
            [Security.Principal.WindowsBuiltInRole] "Administrator")
}

