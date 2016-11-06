$coPath = '%system.teamcity.build.checkoutDir%'

function Unzip-File {
    param(
        [string]$Zip,
        [string]$Dir
    )
    
    ## Extract the ZIP (based on http://serverfault.com/a/201604)
    $shellApp = New-Object -ComObject shell.application
    $zipFile = $shellApp.namespace($Zip)
    mkdir $Dir
    $shellApp.namespace($Dir).Copyhere($zipFile.items())
}


## Retrieve the PS Pester tests from S3
$avFileUri = "https://ci.appveyor.com/api/projects/ebekker/acmesharp/artifacts/ACMESharp/ACMESharp.POSH-test/ACME-posh-pester.zip"
$zipFilePath = "$coPath\ACME-posh-pester.zip"
$destDir = "$coPath\ACME-posh-pester"

Invoke-WebRequest -Uri $avFileUri -OutFile $zipFilePath
Unblock-File -Path $zipFilePath
Unzip-File -Zip $zipFilePath -Dir $destDir

## Retrieve the PS Pester binaries from S3
$avFileUri = "https://ci.appveyor.com/api/projects/ebekker/acmesharp/artifacts/ACMESharp/ACMESharp.POSH-test/ACME-posh-pester-bin.zip"
$zipFilePath = "$coPath\ACME-posh-pester-bin.zip"
$destDir = "$coPath\ACME-posh-pester"

Invoke-WebRequest -Uri $avFileUri -OutFile $zipFilePath
Unblock-File -Path $zipFilePath
Unzip-File -Zip $zipFilePath -Dir $destDir
