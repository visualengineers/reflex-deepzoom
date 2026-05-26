using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ReFlex.Apps.DeepZoom.ViewModels;

public class InfoViewModel
{
    public Version ProgramVersion { get; }
    
    public String ProgramName { get; }
    
    public String ProgramCompany { get; }
    
    public String ProgramCopyright { get; }
    
    public List<AssemblyName> ReferencedAssemblies { get; }

    public InfoViewModel()
    {
        var name = Assembly.GetExecutingAssembly().GetName();
        ProgramVersion = name.Version;
        ProgramName = name.Name;
        var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        ProgramCopyright = versionInfo.LegalCopyright;
        ProgramCompany = versionInfo.CompanyName;
        ReferencedAssemblies = Assembly.GetEntryAssembly()?.GetReferencedAssemblies().ToList();
    }
}