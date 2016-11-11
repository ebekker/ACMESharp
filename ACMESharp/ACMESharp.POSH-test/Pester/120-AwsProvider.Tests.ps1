
. "$PSScriptRoot\000-Common.ps1"


Describe "AwsHandlerTests" {

    Context "AwsS3" {
        $testPath = "$(Join-Path $TestDrive 'test1')"

        $profName = "test_$([DateTime]::Now.ToString('yyyyMMdd_HHmmss'))"
        $provName = "local"
        $vaultParams = @{ RootPath = $testPath; CreatePath = $true }

        It "confirms AWS S3 Handler is available" {
            $ch = Get-ACMEChallengeHandlerProfile -ListChallengeHandlers
            $ch.Contains('aws-s3') | Should Be $true
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
            $dnsId = New-ACMEIdentifier -VaultProfile $profName -Dns $TEST_DNS_ID -Alias dns1

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
            $dnsId = New-ACMEIdentifier -VaultProfile $profName -Dns $TEST_DNS_ID -Alias dns2
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
            $dnsId.Identifier | Should Be $TEST_DNS_ID
            $dnsId.Uri | Should Not BeNullOrEmpty
            $dnsId.Status | Should Be pending
            $dnsId.Expires | Should BeGreaterThan ([DateTime]::Now)
        }
        It "handles the HTTP Challenge via AWS S3 Handler" {
            $awsS3Params = @{
                BucketName = "acmetesting.zyborg.io"
               #ContentType = XXX
               #CannedAcl = 
               #    AuthenticatedRead
               #    AWSExecRead
               #    BucketOwnerFullControl
               #    BucketOwnerRead
               #    LogDeliveryWrite
               #    NoACL
               #    Private
               #    PublicRead
               #    PublicReadWrite
               #Region = "us-east-1"
            }

            try {
                $awsCredProfile = "pesterAwsTestCreds"
                if (Get-AWSCredentials $awsCredProfile) {
                    $awsS3Params.AwsProfileName = $awsCredProfile
                    Write-Host "Using AWS Credential Profile [$awsCredProfile]"
                }
            }
            catch {
                Write-Host "Using ENV VARS for AWS Creds (due to:  $($Error[0]))"
            }

            $authzState = Complete-ACMEChallenge -VaultProfile $profName -IdentifierRef dns1 -ChallengeType http-01 `
                    -Handler awsS3 -HandlerParameters $awsS3Params

            $authzState | Should Not BeNullOrEmpty
        }
        It "submits the HTTP Challenge to be validated" {
            $authzState = Submit-ACMEChallenge -VaultProfile $profName -IdentifierRef dns1 -ChallengeType http-01
        
            $authzState | Should Not BeNullOrEmpty
        }
        It "checks the status of the Identifier verification" {
            $tries = 0
            do {
                $authz = Update-ACMEIdentifier -VaultProfile $profName -IdentifierRef dns1
                $authz | Should Not BeNullOrEmpty
        
                if ($authz.Status -ne "pending") {
                    break
                }
                sleep -Seconds 10 ## Wait some period of time to give the server time to verify
            } while ($tries++ -lt 3)
        
            $authz.Status | Should Be "valid"
        }
        It "requests a new Certificate" {
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
        It "exports Certificate matter" {
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
}
