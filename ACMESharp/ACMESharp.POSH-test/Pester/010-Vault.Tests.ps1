
. "$PSScriptRoot\000-Common.ps1"


Describe "VaultProfileTests" {

    Context "Default Vault Profiles Exit" {
        It "finds built-in default Profiles" {
            $vaultProfiles = Get-ACMEVaultProfile -List

            #if (-not $vaultProfiles.Contains(':sys')) {
            #    throw "Built-in sys Profile not found"
            #}
            #if (-not $vaultProfiles.Contains(':user')) {
            #    throw "Built-in user pProfile not found"
            #}

            [string]::Join(',', $vaultProfiles) | Should Be ':sys,:user'
        }
    }

    Context "Resolve Default Profile" {
        It "matches default Profile to the current elevated admin privilege" {
            $expectedProfileName = switch (Test-IsAdmin) {
                $true { ':sys' }
                $false { ':user' }
            }
            $defaultProfile = Get-ACMEVaultProfile
            $defaultProfile.Name | Should Be $expectedProfileName
        }
    }

    Context "Create And Remove Profile" {
        $testPath = Join-Path $TestDrive 'test1'

        $profName = "test_$([DateTime]::Now.ToString('yyyyMMdd_HHmmss'))"
        $provName = "local"
        $vaultParams = @{ RootPath = $testPath; CreatePath = $false }

        $profRoot = "$($env:LOCALAPPDATA)\ACMESharp\vaultProfiles"
        $profPath = Join-Path $profRoot $profName


        It "creates a new Profile" {
            Test-Path $profPath | Should Be $false
            Set-ACMEVaultProfile -ProfileName $profName -Provider $provName -VaultParameters $vaultParams
            Test-Path $profPath | Should Be $true
        }
        It "removes a new Profile" {
            Set-ACMEVaultProfile -ProfileName $profName -Remove
            Test-Path $profPath | Should Be $false
        }
    }

    Context "Manage Profile with Existing Vault" {
        $testPath = "$(Join-Path $TestDrive 'test1')"

        $profName = "test_$([DateTime]::Now.ToString('yyyyMMdd_HHmmss'))"
        $provName = "local"
        $vaultParams1 = @{ RootPath = $testPath; CreatePath = $false }
        $vaultParams2 = @{ RootPath = $testPath; CreatePath = $true }

        $profRoot = "$($env:LOCALAPPDATA)\ACMESharp\vaultProfiles"
        $profPath = Join-Path $profRoot $profName

        Write-Host "Using Profile Name:  $profName"
        Write-Host "Using Profile Path:  $profPath"
        Write-Host "Using Test Path:     $testPath"

        It "creates a Profile" {
            Test-Path $profPath | Should Be $false
            Set-ACMEVaultProfile -ProfileName $profName -Provider $provName -VaultParameters $vaultParams1
            Test-Path $profPath | Should Be $true
        }
        It "initializes a Vault without CreatePath" {
            Test-Path $testPath | Should Be $false
            { Initialize-ACMEVault -VaultProfile $profName } | Should Throw "Root Path not found"
            Test-Path $testPath | Should Be $false
        }
        It "update a Profile without Force" {
            { Set-ACMEVaultProfile -ProfileName $profName -Provider $provName -VaultParameters $vaultParams2 } |
                    Should Throw ## Can't update existing profile without a -Force
        }
        It "update a Profile with Force" {
            Set-ACMEVaultProfile -ProfileName $profName -Provider $provName -VaultParameters $vaultParams2 -Force
        }
        It "initializes a Vault with CreatePath" {
            Test-Path $testPath | Should Be $false
            Initialize-ACMEVault -VaultProfile $profName
            Test-Path $testPath | Should Be $true
        }
        It "removes a Profile without Force" {
            { Set-ACMEVaultProfile -ProfileName $profName -Remove } | Should Throw
            Test-Path $profPath | Should Be $true
        }
        It "removes a Profile with Force" {
            Set-ACMEVaultProfile -ProfileName $profName -Remove -Force
            Test-Path $profPath | Should Be $false
        }
    }

}

Describe "VaultTests" {

    Context "List and Get Available Challenge Types" {
        It "verifies the Challenge Types" {
            $ct = Get-ACMEChallengeHandlerProfile -ListChallengeTypes
            ($ct | ConvertTo-Json -Compress) | Should Be (@('dns-01', 'http-01') | ConvertTo-Json -Compress)
        }
        It "gets the details of a non-existent Challenge Type" {
            { Get-ACMEChallengeHandlerProfile -GetChallengeType no-such-type } | Should Throw
        }
        It "gets the details of the DNS Challenge Type" {
            $ct = Get-ACMEChallengeHandlerProfile -GetChallengeType dns-01
            $ct | Should Not BeNullOrEmpty
            $ct.SupportedType | Should Be DNS
            $ct.ChallengeType | Should Be dns-01
        }
        It "gets the details of the HTTP Challenge Type" {
            $ct = Get-ACMEChallengeHandlerProfile -GetChallengeType http-01
            $ct | Should Not BeNullOrEmpty
            $ct.SupportedType | Should Be HTTP
            $ct.ChallengeType | Should Be http-01
        }
    }

    Context "List and Get Available Challenge Handlers" {
        It "verifies the Challenge Handlers " {
            $ch = Get-ACMEChallengeHandlerProfile -ListChallengeHandlers
            ($ch | ConvertTo-Json -Compress) | Should Be (@('manual', 'aws-route53', 'aws-s3', 'iis') | ConvertTo-Json -Compress)
        }
        It "gets the details of a non-existent Challenge Handler" {
            { Get-ACMEChallengeHandlerProfile -GetChallengeHandler no-such-type } | Should Throw
        }
        It "gets the details of the MANUAL Challenge Handler" {
            $ct = Get-ACMEChallengeHandlerProfile -GetChallengeHandler manual
            $ct | Should Not BeNullOrEmpty
            $ct.Parameters | Should Not BeNullOrEmpty
            $ct.SupportedTypes | Should Be ([ACMESharp.ACME.ChallengeTypeKind]"DNS,HTTP")
        }
        It "gets the details of the MANUAL Challenge Handler parameters" {
            $ctp = Get-ACMEChallengeHandlerProfile -GetChallengeHandler manual -ParametersOnly
            $ctp | Should Not BeNullOrEmpty
            $ctp.Count | Should Be 3
            ($ctp | % { $_.Name } | ConvertTo-Json -Compress) | Should Be (@("WriteOutPath", "Append", "Overwrite") | ConvertTo-Json -Compress)
        }
    }

    Context "Manage Challenge Handler Profiles" {
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
        It "gets the null collection of Challenge Handler profiles" {
            $lp = Get-ACMEChallengeHandlerProfile -VaultProfile $profName -ListProfiles
            $lp | Should Be $null
        }
        It "gets the details of a non-existent Challenge Handler profile" {
            $ch = Get-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileRef ch1
            $ch | Should Be $null
        }
        It "sets the details of a new Challenge Handler profile" {
            Set-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileName ch1 -ChallengeType dns-01 `
                    -Handler manual -HandlerParameters @{ WriteOutPath = "$testPath\dnsManual" }
            Set-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileName ch2 -ChallengeType http-01 `
                    -Handler manual -HandlerParameters @{ WriteOutPath = "$testPath\httpManual" }

            $ch1 = Get-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileRef ch1
            $ch1 | Should Not BeNullOrEmpty
            $ch1.ProviderType | Should Be ([ACMESharp.Vault.Profile.ProviderType]::CHALLENGE_HANDLER)
            $ch1.ProviderName | Should Be manual
            ($ch1.InstanceParameters | ConvertTo-Json) |
                    Should Be (@{ WriteOutPath = "$testPath\dnsManual" } | ConvertTo-Json)
        }
        It "fails to set an existing Challenge Handler profile" {
            { Set-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileName ch1 `
                    -ChallengeType dns-01 -Handler manual `
                    -HandlerParameters @{ WriteOutPath = "$testPath\manualHandler" } } | Should Throw
            { Set-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileName ch2 `
                    -ChallengeType http-01 -Handler manual `
                    -HandlerParameters @{ WriteOutPath = "$testPath\manualHandler" } } | Should Throw
        }
        It "removes then re-set an existing Challenge Handler profile" {
            Set-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileName ch1 -Remove

            $ch1 = Get-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileRef ch1
            $ch1 | Should Be $null

            Set-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileName ch1 `
                    -ChallengeType dns-01 -Handler manual `
                    -HandlerParameters @{ WriteOutPath = "$testPath\dnsManual.txt" }

            $ch1 = Get-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileRef ch1
            $ch1 | Should Not BeNullOrEmpty
            $ch1.ProviderType | Should Be ([ACMESharp.Vault.Profile.ProviderType]::CHALLENGE_HANDLER)
            $ch1.ProviderName | Should Be manual

            Set-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileName ch2 -Force `
                    -ChallengeType dns-01 -Handler manual `
                    -HandlerParameters @{ WriteOutPath = "$testPath\dnsManual.txt" }

            $ch2 = Get-ACMEChallengeHandlerProfile -VaultProfile $profName -ProfileRef ch2
            $ch2 | Should Not BeNullOrEmpty
            $ch2.ProviderType | Should Be ([ACMESharp.Vault.Profile.ProviderType]::CHALLENGE_HANDLER)
            $ch2.ProviderName | Should Be manual
        }
        It "removes a Profile with Force" {
            Set-ACMEVaultProfile -ProfileName $profName -Remove -Force
        }
    }
}
