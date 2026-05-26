using System;

namespace ReFlex.Apps.DeepZoom.Diagnostics;

public class DiagnosticsData
{
    public static string ApplicationId => "ReFlex.Apps.DeepZoom";

    public string EventTypeDescription { get; }
    
    public long Timestamp { get; }
    
    public string Data1 { get; }
    
    public string Data2 { get; } 
    
    public string Remarks { get; }

    public DiagnosticsData(string eventDesc, string data1 = "", string data2 = "", string remarks = "")
    {
        EventTypeDescription = eventDesc;
        Data1 = data1;
        Data2 = data2;
        Remarks = remarks;
        Timestamp = DateTime.Now.Ticks;
    }
}