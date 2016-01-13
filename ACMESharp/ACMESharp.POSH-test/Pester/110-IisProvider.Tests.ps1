
. "$PSScriptRoot\000-Common.ps1"


function Get-MyIP {
    $ipJson = Invoke-WebRequest -Uri https://api.ipify.org?format=json -UseBasicParsing | ConvertFrom-Json
    $ipJson.ip
}

$r53HostedZoneId = $env:PESTER_AWSR53_HOSTEDZONEID


Describe "IisHandlerTests" {

    Context "Prep" {
        It "prepares some preliminary configuration" {
            $r53HostedZoneId | Should Not BeNullOrEmpty
           #Write-Host $r53HostedZoneId

            $myIp = Get-MyIP
            Write-Host $myIp
            $myIp | Should Not BeNullOrEmpty
            $myIp | Should Match "\d+\.\d+\.\d+\.\d+"

            $TEST_MYIP_DNS_ID | Should Not BeNullOrEmpty
            Write-Host $TEST_MYIP_DNS_ID

            ## For our tests, we use an AWS Route 53-managed DNS domain
            ## and we need to update our test DNS record to point to the
            ## current host's public-facing IP address.  We use the helper
            ## class in the AWS provider to update the DNS Resource Record
            Add-Type -Path $ACME_POSH_PATH\ACMESharp.Providers.AWS.dll
            $r53 = New-Object ACMESharp.Providers.AWS.Route53Helper
            $r53.HostedZoneId = $r53HostedZoneId
            $r53.ResourceRecordTtl = 30

            try {
                $awsCredProfile = "pesterAwsTestCreds"
                if (Get-AWSCredentials $awsCredProfile) {
                    $r53.CommonParams.AwsProfileName = $awsCredProfile
                    Write-Host "Using AWS Credential Profile [$awsCredProfile]"
                }
            }
            catch {
                Write-Host "Using ENV VARS for AWS Creds (due to:  $($Error[0]))"
            }

            ## Make the DNS updates
            $r53EditTag = New-Object System.Collections.Generic.List[string]
            $r53EditTag.Add("Created" +
                    " At [$([datetime]::Now)]" +
                    " On [$([System.Environment]::MachineName)]" +
                    " By [$([System.Environment]::UserName)]")
            Write-Host "Tag:  $r53EditTag"
            $r53.EditARecord($TEST_MYIP_DNS_ID, $myIp)
            $r53.EditTxtRecord($TEST_MYIP_DNS_ID, $r53EditTag)

            ## Pause for a second to let the change propogate
            Write-Host "Taking a breath..."
            sleep -Seconds 5
        }
    }

    <#
    ## Temporarily disabled till we can figure out
    ## the inconsistency on ps.org's build server

    Context "IIS Handler" {
        $testPath = "$(Join-Path $TestDrive 'test1')"

        $profName = "test_$([DateTime]::Now.ToString('yyyyMMdd_HHmmss'))"
        $provName = "local"
        $vaultParams = @{ RootPath = $testPath; CreatePath = $true }

        It "confirms IIS Handler is available" {
            $ch = Get-ACMEChallengeHandlerProfile -ListChallengeHandlers
            $ch.Contains('iis') | Should Be $true
        }
        It "creates a Profile" {
            Set-ACMEVaultProfile -ProfileName $profName -Provider $provName -VaultParameters $vaultParams -Force
        }
        It "initializes the Vault" {
            Initialize-ACMEVault -VaultProfile $profName -BaseService LetsEncrypt-STAGING -Force
        }
        It "adds a new Registration with accepted ToS" {
            $r = New-ACMERegistration -VaultProfile $profName -Contacts mailto:letsencrypt@mailinator.org -AcceptTos
            $v = Get-ACMEVault -VaultProfile $profName

            $r | Should Not BeNullOrEmpty
            $v.Registrations | Should Not BeNullOrEmpty
            $v.Registrations.Count | Should Be 1
            
            ## Cheap way to test for member-wise equality between the returned object
            ## and what was captured in the Vault, compare the serialized forms
            $vr = $v.Registrations[0].Registration
            $r_s = [ACMESharp.Util.JsonHelper]::Save($r, $false)
            $vr_s = [ACMESharp.Util.JsonHelper]::Save($vr, $false)

            $r_s | Should Be $vr_s
        }
        It "adds a new DNS Identifier" {
            $dnsId = New-ACMEIdentifier -VaultProfile $profName -Dns $TEST_MYIP_DNS_ID -Alias dns1

            ## Sanity check some results
            $dnsId | Should Not BeNullOrEmpty
            $dnsId.Status | Should Be pending
            @($dnsId.Challenges).Count | Should BeGreaterThan 0
            @($dnsId.Combinations).Count | Should BeGreaterThan 0

            ## Make sure the expected challenges types that we plan to test with are there
            $dnsId.Challenges | ? { $_.Type -eq "dns-01" } | Should Not BeNullOrEmpty
            $dnsId.Challenges | ? { $_.Type -eq "http-01" } | Should Not BeNullOrEmpty

            $vltId = Get-ACMEIdentifier -VaultProfile $profName -Ref dns1
            $dnsId_s = [ACMESharp.Util.JsonHelper]::Save($dnsId, $false)
            $vltId_s = [ACMESharp.Util.JsonHelper]::Save($vltId, $false)

            $vltId_s| Should Be $dnsId_s

            ## Add a second just for good measure
            $dnsId = New-ACMEIdentifier -VaultProfile $profName -Dns $TEST_MYIP_DNS_ID -Alias dns2
        }
        It "lists Identifiers in Vault" {
            $ids = Get-ACMEIdentifier -VaultProfile $profName

            $ids | Should Not BeNullOrEmpty
            $ids.Count | Should Be 2
            $ids[0].Seq | Should Be 0
            $ids[0].Alias | Should Be dns1
            $ids[0].Status | Should Be pending
        }
        It "gets expected Identifier details in Vault" {
            $dnsId = Get-ACMEIdentifier -VaultProfile $profName -Ref dns1

            $dnsId | Should Not BeNullOrEmpty
            $dnsId.IdentifierType | Should Be dns
            $dnsId.Identifier | Should Be $TEST_MYIP_DNS_ID
            $dnsId.Uri | Should Not BeNullOrEmpty
            $dnsId.Status | Should Be pending
            $dnsId.Expires | Should BeGreaterThan ([DateTime]::Now)
        }
        It "handles the HTTP Challenge via IIS Handler" {
            $handlerParams = @{
                WebSiteRef = "Default Web Site"
            }

            $authzState = Complete-ACMEChallenge -VaultProfile $profName -IdentifierRef dns1 -ChallengeType http-01 `
                    -Handler iis -HandlerParameters $handlerParams

            $authzState | Should Not BeNullOrEmpty

            "C:\inetpub\wwwroot\.well-known\acme-challenge" | Should Exist

            $dnsId = Get-ACMEIdentifier -VaultProfile $profName -Ref dns1
            $dnsId | Should Not BeNullOrEmpty
            $dnsId.Challenges | Should Not BeNullOrEmpty
            
            $httpChallenge = $dnsId.Challenges | ? { $_.Type -eq 'http-01' }
            $httpChallenge | Should Not BeNullOrEmpty
            $httpChallenge.Challenge | Should Not BeNullOrEmpty
            $httpChallenge.Challenge.FileUrl | Should Not BeNullOrEmpty

            Write-Host "Challenge response content URL: [$($httpChallenge.Challenge.FileUrl)]"
        }
        It -Skip "submits the HTTP Challenge to be validated" {
            $authzState = Submit-ACMEChallenge -VaultProfile $profName -IdentifierRef dns1 -ChallengeType http-01
        
            $authzState | Should Not BeNullOrEmpty
        }
        It -Skip "checks the status of the Identifier verification" {
            $tries = 0
            do {
                $authz = Update-ACMEIdentifier -VaultProfile $profName -IdentifierRef dns1
                $authz | Should Not BeNullOrEmpty
        
                if ($authz.Status -ne "pending") {
                    break
                }
                Write-Host "   ...still pending"
                sleep -Seconds 10 ## Wait some period of time to give the server time to verify
            } while ($tries++ -lt 3)
        
            if ($authz.Status -ne 'valid') {
                Write-Warning "WARNING:  INVALID status, pausing..."
                Write-Host "WARNING:  INVALID status, pausing..."
                sleep -s 120
            }

            $authz.Status | Should Be "valid"
        }
        It "cleans up the IIS Handler artifacts" {
            "C:\inetpub\wwwroot\.well-known\acme-challenge" | Should Exist

            $handlerParams = @{
                WebSiteRef = "Default Web Site"
            }

            $authzState = Complete-ACMEChallenge -VaultProfile $profName -IdentifierRef dns1 -ChallengeType http-01 `
                    -Handler iis -HandlerParameters $handlerParams -CleanUp

            $authzState | Should Not BeNullOrEmpty

            "C:\inetpub\wwwroot\.well-known\acme-challenge" | Should Not Exist
        }
        It -Skip "requests a new Certificate" {
            $cert1a = New-ACMECertificate -VaultProfile $profName -IdentifierRef dns1 -Generate -Alias cert1
            $cert1a | Should Not BeNullOrEmpty

            $cert1b = Submit-ACMECertificate -VaultProfile $profName -CertificateRef cert1
            $cert1b | Should Not BeNullOrEmpty
            $cert1b.CertificateRequest | Should Not BeNullOrEmpty
            $cert1b.CertificateRequest.CsrContent | Should Not BeNullOrEmpty

            $cert1c = Update-ACMECertificate -VaultProfile $profName -CertificateRef cert1
            $cert1c | Should Not BeNullOrEmpty
            $cert1c.CertificateRequest | Should Not BeNullOrEmpty
            $cert1c.CertificateRequest.CertificateContent | Should Not BeNullOrEmpty
            $cert1c.Thumbprint | Should Not BeNullOrEmpty
            $cert1c.SerialNumber | Should Not BeNullOrEmpty
            $cert1c.IssuerSerialNumber | Should Not BeNullOrEmpty
        }
        It -Skip "exports Certificate matter" {
            $getParams = @{
                ExportKeyPem          = "$testPath\ExportKeyPem.out"
                ExportCsrPEM          = "$testPath\ExportCsrPEM.out"
                ExportCertificatePEM  = "$testPath\ExportCertificatePEM.out"
                ExportCertificateDER  = "$testPath\ExportCertificateDER.out"
                ExportPkcs12          = "$testPath\ExportPkcs12.out"
            }
            Get-ACMECertificate -VaultProfile $profName -CertificateRef cert1 @getParams

            $getParams.ExportKeyPem         | Should Exist
            $getParams.ExportCsrPEM         | Should Exist
            $getParams.ExportCertificatePEM | Should Exist
            $getParams.ExportCertificateDER | Should Exist
            $getParams.ExportPkcs12         | Should Exist
        }
        It "removes a Profile with Force" {
            Set-ACMEVaultProfile -ProfileName $profName -Remove -Force
        }
    }
    #>
}
