Set-StrictMode -Version Latest

function Test-IsAdmin {
    ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
            [Security.Principal.WindowsBuiltInRole] "Administrator")
}

ipmo "$PSScriptRoot\..\bin\ACMEPowerShell"


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
        #$initCwd = [System.Environment]::CurrentDirectory
        
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

        #[System.Environment]::CurrentDirectory = $initCwd
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
