using System.Windows;
using ReFlex.Core.Common.Components;

namespace ReFlex.Apps.DeepZoom.ViewModels;

public interface IZoomImageViewModel
{
    public ImageSourceViewModel PreviewSource { get; }
    public ImageSourceViewModel ImageSourceOverlay  { get; }
    public ImageSourceViewModel FullImageSource { get; }
    public bool HasOverlay {get; }
    
    public bool IsLoadingInProgress { get; }
    
    public double Scale { get; set; }
    public float ScaleMultiplier { get; }
    
    public Point ZoomCenter  { get; set; }
    
    public Interaction LastInteraction { get; set; }
    
    public double UserControlHeight { get; set; }
    public double UserControlWidth { get; set; }
    
    public bool ShowMiniMap { get; }
    public Size MiniMapSize { get; set; }

    public void ChangeImage(int index);

    public void NextImage();
    
    public void PreviousImage();
}