
Locations that need to be updated with every version bump:
* `/appveyor.yml`
* `/ACMESharp/shared/SharedAssemblyVersionInfo.cs`
* `/ACMESharp/ACMESharp.POSH/ACMESharp.psd1` -- `ModuleVersion`
* `/ACMESharp/ACMESharp.POSH/choco/acmesharp-posh/acmesharp-posh-ea.nuspec` -- `<version>`
* `/ACMESharp/ACMESharp/ACMESharp.POSH-test/choco/acmesharp-posh-all/acmesharp-posh-all.nuspec` -- `<version>`


Building nuget packages with minor revisions
* `/ACMESharp/nuget-build.cmd`
* `/ACMESharp/nuget-build.cmd 3`
