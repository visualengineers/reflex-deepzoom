using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NLog;
using Prism.Events;
using Prism.Mvvm;
using ReFlex.Apps.DeepZoom.Events;

using PixelFormat = System.Windows.Media.PixelFormat;

namespace ReFlex.Apps.DeepZoom.ViewModels;

/// <summary>
/// inspired by: Working around the WPF ImageSource Blues
/// https://weblog.west-wind.com/posts/2024/Jan/03/Working-around-the-WPF-ImageSource-Blues
/// </summary>
public class ImageSourceViewModel : BindableBase, IDisposable
{
    private ImageSource _imageSource;
    private readonly string _imageSourceUri;
    private readonly IEventAggregator _eventAggregator;
    private Size _imageSize;
    private Size _previewImageSize;

    public bool IsLoading => _imageSource == null;

    public Size ImageSize
    {
        get => _imageSize;
        private set => SetProperty(ref _imageSize, value);
    }

    public bool IsPreviewImage { get; set; } = false;

    public ImageSource Image
    {
        get {
            if (_imageSource != null)
                return _imageSource;

            if (!string.IsNullOrEmpty(_imageSourceUri))
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(async () =>
                {
                    try
                    {
                        // code for GetImageSourceAsync() is above
                        _imageSource = await GetImageSourceAsync();
                        _imageSource?.Freeze();
                        if (IsPreviewImage)
                            _eventAggregator.GetEvent<PreviewImageLoadedEvent>().Publish(_imageSource);
                        
                        // _eventAggregator.GetEvent<ImageSourceLoadingFinishedEvent>().Publish(_imageSourceUri);
                    }
                    catch (Exception ex)
                    {
                        LogManager.GetCurrentClassLogger().Error(ex);
                    }
                    finally
                    {
                        RaisePropertyChanged();
                        RaisePropertyChanged(nameof(IsLoading));
                        _eventAggregator.GetEvent<ImageSourceLoadingFinishedEvent>().Publish(_imageSourceUri);
                    }
                        
                }, DispatcherPriority.Background);
            }
            return null;
        }
    }

    public ImageSourceViewModel(string imageSourceUri, IEventAggregator eventAggregator)
    {
        _imageSourceUri = imageSourceUri;
        _eventAggregator = eventAggregator;
    }
    
    private Task<BitmapSource> GetImageSourceAsync()
    {
        if (!string.IsNullOrEmpty(_imageSourceUri))
        {
            var execPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var fname = Path.Combine(Path.GetDirectoryName(execPath) ?? string.Empty, _imageSourceUri);
            
            return LoadImage(fname);

        }
        return null;
    }
    
    public void Dispose()
    {
        _imageSource = null;
    }
    
    private async Task<BitmapSource> LoadImage(string filename, int frameIndex = 0)
    {
        using (var inFile = File.OpenRead(filename))
        {
            
            var decoder = BitmapDecoder.Create(inFile, BitmapCreateOptions.None, BitmapCacheOption.None);
            var result = await Convert(decoder.Frames[frameIndex]);
            
            decoder = null;
            GC.Collect();
            
            return result;
        }
    }

    private async Task<BitmapSource> Convert(BitmapFrame frame)
    { 
        ImageSize = new Size(frame.PixelWidth, frame.PixelHeight);
        
        var stride = frame.PixelWidth * (frame.Format.BitsPerPixel / 8);
        var pixels = new byte[frame.PixelHeight * stride];
        await Task.Run(() =>
        {
            frame.CopyPixels(pixels, stride, 0);
        });

        var bmpSource = BitmapSource.Create(frame.PixelWidth, frame.PixelHeight,
            frame.DpiX, frame.DpiY, frame.Format, frame.Palette, pixels, stride);
        
        return bmpSource;
    }
}