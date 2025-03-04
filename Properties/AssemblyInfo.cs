﻿using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

// Version information for an assembly consists of the following four values:
//      Major Version
//      Minor Version - Improvments
//      Build Number - BugFixes
//      Revision -.Net framework

//---------------------------- WPF Viewer -------------------------------------------------
[assembly: AssemblyInformationalVersion("4.98.2704")]  //Should be equal to the same property of Patagames.Pdf assembly
[assembly: AssemblyVersion("4.29.20." +
#if DOTNET20
"20"
#elif DOTNET30
"30"
#elif DOTNET35
"35"
#elif DOTNET40
"40"
#elif DOTNET45
"45"
#elif DOTNET451
"451"
#elif DOTNET452
"452"
#elif DOTNET46
"46"
#elif DOTNET461
"461"
#elif DOTNET462
"462"
#elif DOTNET47
"47"
#elif DOTNET471
"471"
#elif DOTNET472
"472"
#elif DOTNET48
"48"
#elif DOTNET481
"481"
#elif DOTNET50
"50"
#elif DOTNET60
"60"
#elif DOTNET70
"70"
#elif DOTNET80
"80"
#else
"0"
#endif
)]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if DOTNET20
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 2.0)")]
#elif DOTNET30
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 3.0)")]
#elif DOTNET35
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 3.5)")]
#elif DOTNET40
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 4.0)")]
#elif DOTNET45
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 4.5)")]
#elif DOTNET451
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 4.5.1)")]
#elif DOTNET452
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 4.5.2)")]
#elif DOTNET46
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 4.6)")]
#elif DOTNET461
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 4.6.1)")]
#elif DOTNET462
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 4.6.2)")]
#elif DOTNET47
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 4.7)")]
#elif DOTNET471
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 4.7.1)")]
#elif DOTNET472
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 4.7.2)")]
#elif DOTNET48
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 4.8)")]
#elif DOTNET481
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 4.8.1)")]
#elif DOTNET50
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 5.0)")]
#elif DOTNET60
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 6.0)")]
#elif DOTNET70
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 7.0)")]
#elif DOTNET80
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls (.net 8.0)")]
#else
[assembly: AssemblyTitle("Patagames Pdf.Net SDK - WPF controls")]
#endif
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Patagames Software")]
[assembly: AssemblyProduct("Patagames Pdf.Net SDK")]
[assembly: AssemblyCopyright("Copyright ©  2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
#if !DOTNET20 && !DOTNET30 && !DOTNET35 && !DOTNET40 && !DOTNET45 && !DOTNET451 && !DOTNET452 && !DOTNET46 && !DOTNET461 && !DOTNET462 && !DOTNET47 && !DOTNET471 && !DOTNET472 && !DOTNET48 && !DOTNET481
[assembly: System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
#endif


// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

//In order to begin building localizable applications, set 
//<UICulture>CultureYouAreCodingWith</UICulture> in your .csproj file
//inside a <PropertyGroup>.  For example, if you are using US english
//in your source files, set the <UICulture> to en-US.  Then uncomment
//the NeutralResourceLanguage attribute below.  Update the "en-US" in
//the line below to match the UICulture setting in the project file.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly:ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                             //(used if a resource is not found in the page, 
                             // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                      //(used if a resource is not found in the page, 
                                      // app, or any theme specific resource dictionaries)
)]