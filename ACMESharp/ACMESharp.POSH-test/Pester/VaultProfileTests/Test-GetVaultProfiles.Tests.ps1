$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path) -replace '\.Tests\.', '.'
. "$here\$sut"

Describe "Test-GetVaultProfiles" {
    It "outputs all Vault Profiles" {
		Test-GetVaultProfiles | Should Be @(':sys', ':user')
    }
}
