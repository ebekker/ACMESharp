
Set-StrictMode -Version Latest

if (-not $env:PESTER_PSVER) {
    $env:PESTER_PSVER = $PSVersionTable.PSVersion.Major
}

$TEST_DNS_ID       = "acme-pester.acmetesting.zyborg.io"
$TEST_MYIP_DNS_ID  = "acme-pester-ps$($env:PESTER_PSVER)-myip.acmetesting.zyborg.io"
$TEST_DNS_CHLNG_ID = "acme-pester-ps$($env:PESTER_PSVER)-chlng.acmetesting.zyborg.io"


if (-not (Get-Variable ACME_POSH_PATH -ValueOnly -ErrorAction Ignore)) {
    $ACME_POSH_PATH = "$PSScriptRoot\..\bin\ACMESharp"
}
Write-Host "Resolve ACMESharp POSH Module path to [$ACME_POSH_PATH]"
ipmo $ACME_POSH_PATH


function Test-IsAdmin {
    ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
            [Security.Principal.WindowsBuiltInRole] "Administrator")
}

