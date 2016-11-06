using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ACMESharp Vault")]
[assembly: AssemblyDescription("Vault support for ACMESharp")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("github.com/ebekker/ACMESharp")]
[assembly: AssemblyProduct("ACMESharp.Vault")]
[assembly: AssemblyCopyright("Copyright © 2016 Eugene Bekker. All rights reserved.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("65b4c6e0-27a0-445c-adac-9d8308e2e4a6")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion(ASMINFO.VERSION + ".0")]
[assembly: AssemblyFileVersion(ASMINFO.VERSION + ".0")]

// This is used for the NuSpec version tag replacement
// and is combined with nuget-specific rev and release
[assembly: AssemblyInformationalVersion(ASMINFO.VERSION)]

// ReSharper disable once InconsistentNaming
internal static class ASMINFO
{

    // DON'T FORGET TO UPDATE APPVEYOR.YML
    // ReSharper disable once InconsistentNaming
    public const string VERSION = "0.8.0";
}
