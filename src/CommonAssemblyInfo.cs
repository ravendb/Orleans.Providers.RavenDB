using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;

[assembly: AssemblyCopyright("© RavenDB 2009 - 2026 All rights reserved.")]

[assembly: AssemblyVersion("1.0.8")]
[assembly: AssemblyFileVersion("1.0.8")]
[assembly: AssemblyInformationalVersion("1.0.8-e4bf9a6")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
[assembly: DebuggerDisplay("{ToString(\"O\")}", Target = typeof(DateTime))]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyDelaySign(false)]
[assembly: NeutralResourcesLanguage("en-US")]