An easy way to test this POSH module project, which generates an assembly DLL,
is to configure the local project's "Debug" settings to use the following:

  * Make sure to create a "TestVault" folder in the generated "bin" sub-directory

  * Start Actions > Start External Program (note this is 32-bit PS, see #2 below):
      C:\windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe

  * Start Options > Command line arguments (note the exec policy, see #3 below):
	  -NoExit -Command "Set-ExecutionPolicy -Force -Scope Process -ExecutionPolicy RemoteSigned;ipmo @('..\Debug\ACMESharp', '..\Debug\ACMESharp\ACMESharp-AWS', '..\Debug\ACMESharp\ACMESharp-IIS')"
  * Start Options > Working directory:
      ..\..\TestVault

NOTES:

(1) IGNORE Any warnings that come up about "missing paths"
    for the Working Directory -- the VS UI doesn't resolve
    relative paths, but the build process will (VS2015).

(2) Up above we invoke the 32-bit PS shell because of current limitation
    on the ManagedOpenSSL Nuget package and that it only delivers 32-bit
    versions of the native OpenSSL libraries.  This will be remedied in
    a future update of the ManagedOpenSSL Nuget package.

(3) We issue the ExecutionPolicy command with launching the PS shell
    because the "Installers" modules are written as Script Modules;
	if the default POSH Exec Policy on the local host is at least as
	permissive as "RemoteSigned", then this step can be skipped.

	Also, in the .user build configuration file where the Debug settings
	are stored, you can substitute references to the "Debug" component
	of any path with $(Configuration) to make it work correctly across
	any build configuration -- note this has to be done in the file
	directly, as the UI will escape the non-alpha chars.  Reload the
	project if you make any changes to the build config files directly.
