using System.Windows.Media;

namespace ReFlex.Apps.DeepZoom.Model;

public class ImageData
{
    public string Name { get; set; }
    public string BasePath { get; set; }
    
    public string ImageFullPath { get; set; }
    public string ImagePreviewPath { get; set; }
    public string ImageOverlayPath { get; set; }
    
    public bool HasOverlay { get; set; }
    
    public bool IsActive { get; set; }

}