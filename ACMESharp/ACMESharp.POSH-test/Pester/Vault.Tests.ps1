Set-StrictMode -Version Latest

function Test-IsAdmin {
    ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
            [Security.Principal.WindowsBuiltInRole] "Administrator")
}

if (-not (Get-Variable ACME_POSH_PATH -ValueOnly -ErrorAction Ignore)) {
    $ACME_POSH_PATH = "$PSScriptRoot\..\bin\ACMEPowerShell"
}

ipmo $ACME_POSH_PATH


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
            Set-ACMEVaultProfile -ProfileName $profName -ProviderName $provName -VaultParameters $vaultParams
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
            Set-ACMEVaultProfile -ProfileName $profName -ProviderName $provName -VaultParameters $vaultParams1
            Test-Path $profPath | Should Be $true
        }
        It "initializes a Vault without CreatePath" {
            Test-Path $testPath | Should Be $false
            { Initialize-ACMEVault -VaultProfile $profName } | Should Throw "Root Path not found"
            Test-Path $testPath | Should Be $false
        }
        It "update a Profile without Force" {
            { Set-ACMEVaultProfile -ProfileName $profName -ProviderName $provName -VaultParameters $vaultParams2 } |
                    Should Throw ## Can't update existing profile without a -Force
        }
        It "update a Profile with Force" {
            Set-ACMEVaultProfile -ProfileName $profName -ProviderName $provName -VaultParameters $vaultParams2 -Force
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

    Context "Manage Profile with Existing Vault" {
        $testPath = "$(Join-Path $TestDrive 'test1')"

        $profName = "test_$([DateTime]::Now.ToString('yyyyMMdd_HHmmss'))"
        $provName = "local"
        $vaultParams = @{ RootPath = $testPath; CreatePath = $true }

        It "creates a Profile" {
            Set-ACMEVaultProfile -ProfileName $profName -ProviderName $provName -VaultParameters $vaultParams -Force
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
}

<#
$isAdmin = [ACMESharp.Util.SysHelper]::IsElevatedAdmin()

$tempFile = [System.IO.Path]::GetTempFileName()
$tempRoot = "$tempFile-vault"



Initialize-ACMEVault -VaultProfile test1 -BaseService LetsEncrypt-STAGING


ipmo  "$PSScriptRoot\..\bin\ACMEPowerShell"

$vaultProfiles = Get-ACMEVaultProfile -List
$vaultProfiles | Should

Set-ACMEVaultProfile -ProfileName test1 -ProviderName local -VaultParameters @{ RootPath = 'C:\temp\$tempRoot'; CreatePath = $false }




echo Set-ACMEVaultProfile -ProfileName test2 -ProviderName local -VaultParameters @{ RootPath = 'C:\temp\vault-test2'; CreatePath = $true }
Set-ACMEVaultProfile -ProfileName test2 -ProviderName local -VaultParameters @{ RootPath = 'C:\temp\vault-test2'; CreatePath = $true }
Set-ACMEVaultProfile -ProfileName test2 -Remove
Set-ACMEVaultProfile -ProfileName test2 -ProviderName local -VaultParameters @{ RootPath = 'C:\temp\vault-test2'; CreatePath = $true }
Set-ACMEVaultProfile -ProfileName test2 -ProviderName local -VaultParameters @{ RootPath = 'C:\temp\vault-test2'; CreatePath = $true }
Set-ACMEVaultProfile -ProfileName test2 -ProviderName local -VaultParameters @{ RootPath = 'C:\temp\vault-test2'; CreatePath = $false }
Set-ACMEVaultProfile -ProfileName test2 -ProviderName local -VaultParameters @{ RootPath = 'C:\temp\vault-test2'; CreatePath = $false } -Force
Set-ACMEVaultProfile -ProfileName test2 -ProviderName local -VaultParameters @{ RootPath = 'C:\temp\vault-test2'; CreatePath = $true }
Set-ACMEVaultProfile -ProfileName test2 -ProviderName local -VaultParameters @{ RootPath = 'C:\temp\vault-test2'; CreatePath = $true } -Force
Initialize-ACMEVault -VaultProfile test2
Initialize-ACMEVault -VaultProfile test2
Initialize-ACMEVault -VaultProfile test2 -BaseService LetsEncrypt-STAGING
Initialize-ACMEVault -VaultProfile test2 -BaseService LetsEncrypt-STAGING -Force
Set-ACMEVaultProfile -ProfileName test2 -Remove
Set-ACMEVaultProfile -ProfileName test2 -Remove -Force
#>
