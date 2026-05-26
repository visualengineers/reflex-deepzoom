using System;
using System.Windows;
using Prism.Events;
using Prism.Mvvm;
using ReFlex.Apps.DeepZoom.Events;
using ReFlex.Apps.DeepZoom.Events.EventData;
using ReFlex.Apps.DeepZoom.Model;
using ReFlex.Core.Common.Components;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace ReFlex.Apps.DeepZoom.ViewModels;

public abstract class ZoomImageViewModelBase: BindableBase, IZoomImageViewModel, IDisposable
{
    protected readonly IEventAggregator EventAggregator;
    private readonly DataRepository _dataRepository;
    
    private ImageSourceViewModel _previewSourceVm;
    private ImageSourceViewModel _imageSourceOverlayVm;
    private ImageSourceViewModel _imageSourceVm;
   
    private Size _userControlSize;
    
    protected double ScaleValue;
    protected Point ZoomCenterValue;
    protected int CurrentImageIndex;
    protected Size MiniMapSizeValue;
    private double _overlayOpacity;
    private Size _aspectRatioOffset;

    public ImageSourceViewModel PreviewSource
    {
        get => _previewSourceVm;
        set => SetProperty(ref _previewSourceVm, value);
    }

    public ImageSourceViewModel ImageSourceOverlay
    {
        get => _imageSourceOverlayVm;
        set => SetProperty(ref _imageSourceOverlayVm, value);
    }

    public ImageSourceViewModel FullImageSource
    {
        get => _imageSourceVm;
        set => SetProperty(ref _imageSourceVm, value);
    }
    
    public virtual double Scale
    {
        get => ScaleValue;
        set
        {
            SetProperty(ref ScaleValue, value);
            RaisePropertyChanged(nameof(ShowMiniMap));
            RaisePropertyChanged(nameof(ScaleLabelText));
        }
    }

    public string ScaleLabelText => $"{ScaleValue:#0.00} x";

    public virtual Point ZoomCenter
    {
        get => ZoomCenterValue;
        set => SetProperty(ref ZoomCenterValue, value);
    }

    public double OverlayOpacity
    {
        get => _overlayOpacity;
        protected set => SetProperty(ref _overlayOpacity, value);
    }

    /// <summary>
    /// Scaling Factor to reduce the maximum Zoom
    /// </summary>
    public float ScaleMultiplier { get; protected init; }
    
    /// <summary>
    /// The Zoom range based on the ratio between user control size and full size image (max zoom goes down to 1px at 100%)
    /// </summary>
    public float NativeZoomRange { get; private set; }
    
    /// <summary>
    /// Resulting Zoom Range used in the Application 
    /// </summary>
    public float EffectiveZoomRange => ScaleMultiplier * NativeZoomRange;

    public abstract Interaction LastInteraction { get; set; }

    public double UserControlHeight
    {
        get => _userControlSize.Height;
        set
        {
            _userControlSize.Height = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ZoomCenter));
        }
    }

    public double UserControlWidth
    {
        get => _userControlSize.Width;
        set
        {
            _userControlSize.Width = value;
            RaisePropertyChanged();
        }
    }

    public Size AspectRatioOffset
    {
        get => _aspectRatioOffset;
        set => SetProperty(ref _aspectRatioOffset, value);
    }

    public virtual bool ShowMiniMap
    {
        get => ScaleValue > 1.0;
    }

    public virtual Size MiniMapSize
    {
        get => MiniMapSizeValue;
        set => SetProperty(ref MiniMapSizeValue, value);
    }
    

    public bool IsLoadingInProgress
    {
        get { return (PreviewSource?.IsLoading ?? true) 
                || (FullImageSource?.IsLoading ?? true) 
                || (HasOverlay && (ImageSourceOverlay?.IsLoading ?? true)); 
        }
    }
    
    public bool HasOverlay { get; private set; }

    protected ZoomImageViewModelBase(IEventAggregator eventAggregator, DataRepository repository)
    {
        EventAggregator = eventAggregator;
        EventAggregator.GetEvent<InteractionReceivedEvent>().Subscribe(OnInteractionReceived);
        EventAggregator.GetEvent<ContentResizeEvent>().Subscribe(OnContentResized);
        EventAggregator.GetEvent<ImageSourceLoadingFinishedEvent>().Subscribe(OnImageSourceLoadingFinished);
        EventAggregator.GetEvent<RequestImageChangeEvent>().Subscribe(OnImageChangeRequested);
        
        NotifyImageLoadingStatus();
        
        _dataRepository = repository;
        
        MiniMapSizeValue = new Size(320, 180);
        
        ChangeImage(repository.CurrentIndex);
        CurrentImageIndex = repository.CurrentIndex;
        
        if (Application.Current?.MainWindow == null) 
            return;
            
        UserControlWidth = Application.Current.MainWindow.Width;
        UserControlHeight = Application.Current.MainWindow.Height;
        Application.Current.MainWindow.SizeChanged += MainWindowOnSizeChanged;
    }

    private void MainWindowOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UserControlWidth = e.NewSize.Width;
        UserControlHeight = e.NewSize.Height;
    }

    public void ChangeImage(int imageIndex = 0)
    {
        var validIndex = imageIndex >= 0 && _dataRepository.Images.ImageData.Count > imageIndex;
        if (!validIndex) 
            return;

        _dataRepository.CurrentIndex = imageIndex;
        
        DisposeImages();

        var image = _dataRepository.Images.ImageData[imageIndex];
        
        HasOverlay = image.HasOverlay;
        
        EventAggregator.GetEvent<DataSetChangedEvent>().Publish(image);
        
        PreviewSource = new ImageSourceViewModel($"{image.BasePath}/{image.ImagePreviewPath}", EventAggregator) { IsPreviewImage = true };
        FullImageSource = new ImageSourceViewModel($"{image.BasePath}/{image.ImageFullPath}", EventAggregator);

        if (image.HasOverlay)
        {
            ImageSourceOverlay = new ImageSourceViewModel($"{image.BasePath}/{image.ImageOverlayPath}",
                EventAggregator);
        }
        else
        {
            ImageSourceOverlay?.Dispose();
            ImageSourceOverlay = null;
            RaisePropertyChanged(nameof(ImageSourceOverlay));
        }
    }

    private void DisposeImages()
    {
        PreviewSource?.Dispose();
        PreviewSource = null;
        RaisePropertyChanged(nameof(PreviewSource));
        
        ImageSourceOverlay?.Dispose();
        ImageSourceOverlay = null;
        RaisePropertyChanged(nameof(ImageSourceOverlay));
        
        FullImageSource?.Dispose();
        FullImageSource = null;
        RaisePropertyChanged(nameof(FullImageSource));
        
        GC.Collect();
    }

    public void NextImage()
    {
        if (IsLoadingInProgress)
            return;
        
        CurrentImageIndex += 1;
        if (CurrentImageIndex > _dataRepository.Images.ImageData.Count - 1)
            CurrentImageIndex = 0;
        ChangeImage(CurrentImageIndex);
    }
    
    public void PreviousImage()
    {
        if (IsLoadingInProgress)
            return;
        
        CurrentImageIndex -= 1;
        if (CurrentImageIndex < 0) 
            CurrentImageIndex = _dataRepository.Images.ImageData.Count - 1;
        ChangeImage(CurrentImageIndex);
    }
    
    public virtual void Dispose()
    {
        EventAggregator.GetEvent<InteractionReceivedEvent>().Unsubscribe(OnInteractionReceived);
        EventAggregator.GetEvent<ContentResizeEvent>().Unsubscribe(OnContentResized);
        EventAggregator.GetEvent<ImageSourceLoadingFinishedEvent>().Unsubscribe(OnImageSourceLoadingFinished);
        EventAggregator.GetEvent<RequestImageChangeEvent>().Unsubscribe(OnImageChangeRequested);
        
        if (Application.Current?.MainWindow != null)
            Application.Current.MainWindow.SizeChanged -= MainWindowOnSizeChanged;
    }
    
    private void UpdateAspectRatioOffset()
    {
        if (PreviewSource.ImageSize.Height == 0 || PreviewSource.ImageSize.Width == 0 || UserControlWidth == 0 || UserControlHeight == 0)
            return;
        
         var aspectRatioImage = PreviewSource.ImageSize.Width / PreviewSource.ImageSize.Height;
         var aspectRatioUserControl = UserControlWidth / UserControlHeight;

         if (aspectRatioImage < aspectRatioUserControl)
         {
             // window ist wider than image: image ist scaled to the height of the window
             var scale = UserControlHeight / PreviewSource.ImageSize.Height;
             var scaledWidth = PreviewSource.ImageSize.Width * scale;

             var differenceWidth = UserControlWidth - scaledWidth;
             AspectRatioOffset = new Size(0.5 * differenceWidth, 0);
         }
         else
         {
             // window ist higher than image: image ist scaled to the width of the window
             var scale = UserControlWidth / PreviewSource.ImageSize.Width;
             var scaledHeight = PreviewSource.ImageSize.Height * scale;
             
             var differenceHeight = UserControlHeight - scaledHeight; 
             
             AspectRatioOffset = new Size(0, 0.5 * differenceHeight);
         }
     }

    private void UpdateZoomRange()
    {
        var zoomFactorX = FullImageSource.ImageSize.Width / UserControlWidth;
        var zoomFactorY = FullImageSource.ImageSize.Height / UserControlHeight;
        
        NativeZoomRange = zoomFactorX > zoomFactorY ? (float) zoomFactorY : (float)zoomFactorX;
        RaisePropertyChanged(nameof(NativeZoomRange));
        RaisePropertyChanged(nameof(EffectiveZoomRange));
        RaisePropertyChanged(nameof(ScaleLabelText));
    }

    private void OnInteractionReceived(Interaction interaction) => LastInteraction = interaction;

    private void OnImageChangeRequested(ImageChangeRequestArguments args)
    {
        switch (args.Type)
        {
            case ImageChangeRequestType.PreviousImage:
                PreviousImage();
                break;
            case ImageChangeRequestType.NextImage:
                NextImage();
                break;
            case ImageChangeRequestType.Offset:
                ChangeImage(CurrentImageIndex + args.Parameter);
                break;
            case ImageChangeRequestType.Index:
                ChangeImage(args.Parameter);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void NotifyImageLoadingStatus()
    {
         RaisePropertyChanged(nameof(IsLoadingInProgress));
         if (!IsLoadingInProgress)
         {
             UpdateAspectRatioOffset();
             UpdateZoomRange();
         }

         EventAggregator.GetEvent<ImageComponentInitializationStateChangedEvent>().Publish(!IsLoadingInProgress);
    }
    
    private void OnImageSourceLoadingFinished(string imagePath) => NotifyImageLoadingStatus();
    
    private void OnContentResized(Size windowSize)
    {
        UserControlWidth = windowSize.Width;
        UserControlHeight = windowSize.Height;
        
        UpdateAspectRatioOffset();
        UpdateZoomRange();
    }
    
}