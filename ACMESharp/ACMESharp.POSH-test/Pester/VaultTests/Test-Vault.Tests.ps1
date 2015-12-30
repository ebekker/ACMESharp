$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path) -replace '\.Tests\.', '.'
. "$here\$sut"

Describe "Test-Vault" {
	Context "When 'Force' is not specified" {
		It "initializes a new default Vault Profile" {
			{ Test-Vault } | Should Throw
		}
	}
}
