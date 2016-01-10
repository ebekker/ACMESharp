
. "$PSScriptRoot\000-Common.ps1"


Describe "RegTests" {

    $testPath = "$(Join-Path $TestDrive 'test1')"

    $profName = "test_$([DateTime]::Now.ToString('yyyyMMdd_HHmmss'))"
    $provName = "local"
    $vaultParams = @{ RootPath = $testPath; CreatePath = $true }

    It "creates a Profile" {
        Set-ACMEVaultProfile -ProfileName $profName -Provider $provName -VaultParameters $vaultParams -Force
    }
    It "initializes the Vault" {
        Initialize-ACMEVault -VaultProfile $profName -BaseService LetsEncrypt-STAGING -Force
    }
    It "retrieves the Vault" {
        $v = Get-ACMEVault -VaultProfile $profName

        $v | Should Not BeNullOrEmpty
        $v.BaseService | Should Be LetsEncrypt-STAGING
        $v.BaseUri | Should Be https://acme-staging.api.letsencrypt.org/
    }
    It "adds a Registration" {
        $r = New-ACMERegistration -VaultProfile $profName -Contacts mailto:letsencrypt@mailinator.org
        $v = Get-ACMEVault -VaultProfile $profName

        $r | Should Not BeNullOrEmpty
        $v.Registrations | Should Not BeNullOrEmpty
        $v.Registrations.Count | Should Be 1
            
        ## Cheap way to test for member-wise equality between the returned object
        ## and what was captured in the Vault, compare the serialized forms
        $vr = $v.Registrations[0].Registration
        ## This doesn't work because the deserialized form of some objects
        ## are slightly different (e.g. dynamic object vs. JObject)
        #    $r_s = $r | Out-String -Width 100
        #    $vr_s = $vr | Out-String -Width 100
        ## This doesn't work because the PS native JSON serialization
        ## doesn't understand JSON.NET types or annotations
        #    $r_s = ConvertTo-Json $r
        #    $vr_s = ConvertTo-Json $vr
        $r_s = [ACMESharp.Util.JsonHelper]::Save($r, $false)
        $vr_s = [ACMESharp.Util.JsonHelper]::Save($vr, $false)

        $r_s | Should Be $vr_s
    }
    It "adds an Identifier before accepting ToS" {
        { New-ACMEIdentifier -VaultProfile $profName -Dns example.com -Alias dns1 } | Should Throw
        $err = $Error[0]
        $err.Exception.Response.StatusCode | Should Be "Forbidden"
    }
    It "updates Registration with contact and ToS" {
        $r = Update-ACMERegistration -VaultProfile $profName -Contacts mailto:letsencrypt2@mailinator.org -AcceptTos
        $r.TosLinkUri | Should Be $r.TosAgreementUri
    }
    ## Can't seem to find a black-listed DNS to test this
    #It "adds an blacklisted Identifier" {
    #    { New-ACMEIdentifier -VaultProfile $profName -Dns example.com -Alias dns1 } | Should Throw
    #    $err = $Error[0]
    #    $err.Exception.Response.StatusCode | Should Be "Forbidden"
    #}
    It "removes a Profile with Force" {
        Set-ACMEVaultProfile -ProfileName $profName -Remove -Force
    }
}

Describe "ChallengeTests" {

    $testPath = "$(Join-Path $TestDrive 'test1')"

    $profName = "test_$([DateTime]::Now.ToString('yyyyMMdd_HHmmss'))"
    $provName = "local"
    $vaultParams = @{ RootPath = $testPath; CreatePath = $true }

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
    It "handles the DNS challenge" {
        $ch1 = Complete-ACMEChallenge -VaultProfile $profName -Ref dns1 -ChallengeType dns-01 `
                -Handler manual -HandlerParameters @{ WriteOutPath="$testPath\manualDns.out" }

        "$testPath\manualDns.out" | Should Exist
        $x = [System.IO.File]::ReadAllText("$testPath\manualDns.out")
        #Write-Host $x
            
    }
    It "handles the HTTP challenge" {
        $ch1 = Complete-ACMEChallenge -VaultProfile $profName -Ref dns1 -ChallengeType http-01 `
                -Handler manual -HandlerParameters @{ WriteOutPath="$testPath\manualHttp.out" }

        "$testPath\manualDns.out" | Should Exist
        $x = [System.IO.File]::ReadAllText("$testPath\manualHttp.out")
        #Write-Host $x
    }
    It "fails to handle the HTTP challenge due to existing response" {
        "$testPath\manualDns.out" | Should Exist
        { $ch1 = Complete-ACMEChallenge -VaultProfile $profName -Ref dns1 -ChallengeType http-01 -Repeat  `
                -Handler manual -HandlerParameters @{ WriteOutPath="$testPath\manualHttp.out" } } | Should Throw
    
    }
    It "appends/overwrites the HTTP challenge" {
        "$testPath\manualDns.out" | Should Exist
        $existing = [System.IO.File]::ReadAllText("$testPath\manualHttp.out")

        $ch1 = Complete-ACMEChallenge -VaultProfile $profName -Ref dns1 -ChallengeType http-01 -Repeat `
                -Handler manual -HandlerParameters @{ WriteOutPath="$testPath\manualHttp.out"; Append = $true }

        "$testPath\manualDns.out" | Should Exist
        $x = [System.IO.File]::ReadAllText("$testPath\manualHttp.out")
        #Write-Host $x
        $x.StartsWith($existing) | Should Be $true

        sleep -Seconds 1 ## Need to sleep for a sec to guarantee we don't have the same timestamp
        $ch1 = Complete-ACMEChallenge -VaultProfile $profName -Ref dns1 -ChallengeType http-01 -Repeat `
                -Handler manual -HandlerParameters @{ WriteOutPath="$testPath\manualHttp.out"; Overwrite = $true }

        "$testPath\manualDns.out" | Should Exist
        $x = [System.IO.File]::ReadAllText("$testPath\manualHttp.out")
        #Write-Host $x
        $x.StartsWith($existing) | Should Be $false

        $existingWoTime = $existing -replace "Handle Time.+\]",""
        $xWoTime = $x -replace "Handle Time.+\]",""

        #Write-Host $xWoTime
        #Write-Host $existingWoTime
        $xWoTime | Should Be $existingWoTime
    }
    It "checks that DNS Handler inline and profile methods produce the same results" {
        "$testPath\manualDns.out" | Should Exist
        $existing = [System.IO.File]::ReadAllText("$testPath\manualDns.out")

        Set-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileName ch1 -ChallengeType dns-01 `
                -Handler manual -HandlerParameters @{ WriteOutPath = "$testPath\manualDnsProfile.out" }

        $ch1 = Complete-ACMEChallenge -VaultProfile $profName -Ref dns1 -Repeat `
                -HandlerProfile ch1

        "$testPath\manualDnsProfile.out" | Should Exist
        $x = [System.IO.File]::ReadAllText("$testPath\manualDnsProfile.out")

        $existingWoTime = $existing -replace "Handle Time.+\]",""
        $xWoTime = $x -replace "Handle Time.+\]",""

        $xWoTime | Should Be $existingWoTime
    }
    It "checks that HTTP Handler inline and profile methods produce the same results" {
        "$testPath\manualHttp.out" | Should Exist
        $existing = [System.IO.File]::ReadAllText("$testPath\manualHttp.out")

        Set-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileName ch2 -ChallengeType http-01 `
                -Handler manual -HandlerParameters @{ WriteOutPath = "$testPath\manualHttpProfile.out" }

        $ch2 = Complete-ACMEChallenge -VaultProfile $profName -Ref dns1 -Repeat `
                -HandlerProfile ch2

        "$testPath\manualHttpProfile.out" | Should Exist
        $x = [System.IO.File]::ReadAllText("$testPath\manualHttpProfile.out")

        $existingWoTime = $existing -replace "Handle Time.+\]",""
        $xWoTime = $x -replace "Handle Time.+\]",""

        $xWoTime | Should Be $existingWoTime
    }
    It "removes a Profile with Force" {
        Set-ACMEVaultProfile -ProfileName $profName -Remove -Force
    }
}
