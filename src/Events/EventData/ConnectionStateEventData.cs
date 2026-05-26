using System;

namespace ReFlex.Apps.DeepZoom.Events.EventData;

public class ConnectionStateEventData
{
    public Guid Id { get; set; }
    
    public bool IsConnected { get; set; }
    
    public string Address { get; set; }
    
    public string StateMsg { get; set; }

    public int Frame { get; set; }

}