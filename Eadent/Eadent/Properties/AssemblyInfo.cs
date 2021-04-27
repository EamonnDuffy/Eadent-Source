using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Eadent")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Eadent")]
[assembly: AssemblyCopyright("Copyright © 2003-2021 Eadent. All Rights Reserved.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("c3992656-0644-4527-ace6-9a66ab22198c")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion(AssemblyInfo.VersionMajor + "." + AssemblyInfo.VersionMinor + "." + AssemblyInfo.VersionBuild + "." + AssemblyInfo.VersionRevision)]
[assembly: AssemblyFileVersion(AssemblyInfo.VersionMajor + "." + AssemblyInfo.VersionMinor + "." + AssemblyInfo.VersionBuild + "." + AssemblyInfo.VersionRevision)]

internal class AssemblyInfo
{
    internal const int CopyrightStartYear = 2003;

    // Keep the Version as components so that code can decide how much of the Version to use.
    internal const string VersionMajor = "2";
    internal const string VersionMinor = "0";
    internal const string VersionBuild = "0";
    internal const string VersionRevision = "0";

    internal const string Domain = "www.Eadent.com"; // TODO: Consider getting from Configuration?
}

