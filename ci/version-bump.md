
Locations that need to be updated with every version bump:
* `/appveyor.yml`
* `/ACMESharp/shared/SharedAssemblyVersionInfo.cs`
* `/ACMESharp/ACMESharp.POSH/ACMESharp.psd1` -- `ModuleVersion`
* `/ACMESharp/ACMESharp.Providers.AWS        /ACMESharp.Providers.*.psd1` -- `ModuleVersion`
* `/ACMESharp/ACMESharp.Providers.CloudFlare /ACMESharp.Providers.*.psd1` -- `ModuleVersion`
* `/ACMESharp/ACMESharp.Providers.IIS        /ACMESharp.Providers.*.psd1` -- `ModuleVersion`
* `/ACMESharp/ACMESharp.Providers.Windows    /ACMESharp.Providers.*.psd1` -- `ModuleVersion`

NO LONGER MAINTAINED - choco not supported
* `/ACMESharp/ACMESharp.POSH/choco/acmesharp-posh/acmesharp-posh-ea.nuspec` -- `<version>`
* `/ACMESharp/ACMESharp/ACMESharp.POSH-test/choco/acmesharp-posh-all/acmesharp-posh-all.nuspec` -- `<version>`

~~Child Projects that have their own version files~~: (no longer true till after 1.0 release)
* `/ACMESharp/ACMESharp.Vault/Properties/AssemblyInfo.cs`
* `/ACMESharp/ACMESharp.Providers.AWS/Properties/AssemblyInfo.cs`
* `/ACMESharp/ACMESharp.Providers.CloudFlare/Properties/AssemblyInfo.cs`
* `/ACMESharp/ACMESharp.Providers.IIS/Properties/AssemblyInfo.cs`
* `/ACMESharp/ACMESharp.Providers.Windows/Properties/AssemblyInfo.cs`


Building nuget packages with minor revisions
* `/ACMESharp/nuget-build.cmd`
* `/ACMESharp/nuget-build.cmd 3`
