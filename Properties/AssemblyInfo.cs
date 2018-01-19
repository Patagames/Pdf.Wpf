using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if DOTNET20
[assembly: AssemblyTitle("Pdfium.Net SDK - WPF controls (.net 2.0)")]
#elif DOTNET30
[assembly: AssemblyTitle("Pdfium.Net SDK - WPF controls (.net 3.0)")]
#elif DOTNET35
[assembly: AssemblyTitle("Pdfium.Net SDK - WPF controls (.net 3.5)")]
#elif DOTNET40
[assembly: AssemblyTitle("Pdfium.Net SDK - WPF controls (.net 4.0)")]
#elif DOTNET45
[assembly: AssemblyTitle("Pdfium.Net SDK - WPF controls (.net 4.5)")]
#elif DOTNET451
[assembly: AssemblyTitle("Pdfium.Net SDK - WPF controls (.net 4.5.1)")]
#elif DOTNET452
[assembly: AssemblyTitle("Pdfium.Net SDK - WPF controls (.net 4.5.2)")]
#elif DOTNET46
[assembly: AssemblyTitle("Pdfium.Net SDK - WPF controls (.net 4.6)")]
#elif DOTNET461
[assembly: AssemblyTitle("Pdfium.Net SDK - WPF controls (.net 4.6.1)")]
#elif DOTNET462
[assembly: AssemblyTitle("Pdfium.Net SDK - WPF controls (.net 4.6.2)")]
#elif DOTNET47
[assembly: AssemblyTitle("Pdfium.Net SDK - WPF controls (.net 4.7)")]
#else
[assembly: AssemblyTitle("Pdfium.Net SDK - WPF controls")]
#endif
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Patagames Software")]
[assembly: AssemblyProduct("Pdfium.Net SDK - WPF controls")]
[assembly: AssemblyCopyright("Copyright ©  2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

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


// Version information for an assembly consists of the following four values:
//      Major Version
//      Minor Version - Improvments
//      Build Number - BugFixes
//      Revision -.Net framework

[assembly: AssemblyInformationalVersion("3.14.2704")]  //Should be equal to save property of Patagames.Pdf assembly
[assembly: AssemblyVersion("3.14.1."+
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
#else
"0"
#endif
)]