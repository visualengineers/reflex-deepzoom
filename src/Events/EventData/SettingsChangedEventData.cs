using System.Windows;
using ReFlex.Apps.DeepZoom.Model;

namespace ReFlex.Apps.DeepZoom.Events.EventData;

public class SettingsChangedEventData
{
    public LensShape LensShape { get; set; }
    public Size LensOffset { get; set; }
    public double LensRadius { get; set; }
    public double LensBorderWidth { get; set; }
    
}