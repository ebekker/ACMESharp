## Set these up which are used by AWS PS calls during the Pester tests below
$env:AWS_ACCESS_KEY_ID      = '%iamAccessKey%'
$env:AWS_SECRET_ACCESS_KEY  = '%iamSecretKey%'

try {
    $coPath = '%system.teamcity.build.checkoutDir%'

    ## This EnvVar is looked for by the Pester tests for ipmo
    #$ACME_POSH_PATH = "$coPath\ACME-posh\Debug\ACMESharp"
    $ACME_POSH_PATH = "$coPath\ACME-posh-pester\ACMESharp"

    Import-Module Pester -ErrorAction Stop

    $xml = Join-Path $coPath Test.v4.xml
    $result = Invoke-Pester -Path $coPath -OutputFile $xml -OutputFormat NUnitXml -PassThru -Strict -ErrorAction Stop

    if ($result.FailedCount -gt 0) {
        throw "$($result.FailedCount) tests did not pass."
    }
}
catch {
    Write-Error -ErrorRecord $_
    exit 1
}