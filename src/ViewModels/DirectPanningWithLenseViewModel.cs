using System;
using System.Windows;
using System.Windows.Media;
using Prism.Events;
using ReFlex.Apps.DeepZoom.Events;
using ReFlex.Apps.DeepZoom.Events.EventData;
using ReFlex.Apps.DeepZoom.Model;
using ReFlex.Core.Common.Components;

namespace ReFlex.Apps.DeepZoom.ViewModels
{
    public class DirectPanningWithLenseViewModel : ZoomImageViewModelBase
    {
        private double _lensRadius;
        private Size _lensOffset;
        private double _lensBorderWidth;
        private Brush _lensBorderColor;
        private LensShape _lensShape;
        private bool _isLensVisible;
        private Interaction _lastInteraction;

        public double LenseRadius
        {
            get => _lensRadius;
            set
            {
                SetProperty(ref _lensRadius, value);
                RaisePropertyChanged(nameof(ScaledLensDiameter));
                RaisePropertyChanged(nameof(ScaledLensRadius));
            }
        }
        
        public double ScaledLensRadius => Scale > 0 ? LenseRadius / Scale : 0.0;
        
        public double ScaledLensDiameter => 2.0 * ScaledLensRadius;
        
        public Size LensOffset { get => _lensOffset; set => SetProperty(ref _lensOffset, value); }
        
        public Brush LensBorderColor { get => _lensBorderColor; set => SetProperty(ref _lensBorderColor, value); }
        
        public Double LensBorderWidth { get => _lensBorderWidth; set => SetProperty(ref _lensBorderWidth, value); }
        
        public LensShape LensShape { get => _lensShape; set => SetProperty(ref _lensShape, value); }

        public bool IsLensVisible
        {
            get => _isLensVisible && !IsLoadingInProgress;
            set
            {
                SetProperty(ref _isLensVisible, value);
                RaisePropertyChanged(nameof(IsOverlayVisible));
                RaisePropertyChanged(nameof(ShowCircleLens));
                RaisePropertyChanged(nameof(ShowRectangleLens));
            }
        }
        
        public bool ShowRectangleLens => IsLensVisible && LensShape == LensShape.Rectangle;
        
        public bool ShowCircleLens => IsLensVisible && LensShape == LensShape.Circle;
        

        public bool IsOverlayVisible => !HasOverlay || _isLensVisible && HasOverlay;

        public override double Scale
        {
            get => ScaleValue;
            set
            {
                SetProperty(ref ScaleValue, value);
                IsLensVisible = ScaleValue > 1;
                RaisePropertyChanged(nameof(IsLensVisible));
                RaisePropertyChanged(nameof(ScaledLensDiameter));
                RaisePropertyChanged(nameof(ScaledLensRadius));
                RaisePropertyChanged(nameof(ShowCircleLens));
                RaisePropertyChanged(nameof(ShowRectangleLens));
                RaisePropertyChanged(nameof(ScaleLabelText));
            }
        }

        public override Interaction LastInteraction
        {
            get => _lastInteraction;
            set
            {
                SetProperty(ref _lastInteraction, value);

                ZoomCenter = new Point(
                    _lastInteraction.Position.X * UserControlWidth - AspectRatioOffset.Width,
                    _lastInteraction.Position.Y * UserControlHeight - AspectRatioOffset.Height
                );

                Scale = 1 + _lastInteraction.Position.Z * EffectiveZoomRange;

                OverlayOpacity = _lastInteraction.Position.Z * 2.0;
            }
        }

        public DirectPanningWithLenseViewModel(IEventAggregator eventAggregator, DataRepository repository)
            : base(eventAggregator, repository)
        {
            ScaleMultiplier = Properties.Settings.Default.ScaleMultiplicator;
            LenseRadius = Properties.Settings.Default.LensRadius;
            LensOffset = new Size(Properties.Settings.Default.LensOffset.Width,
                Properties.Settings.Default.LensOffset.Height);
            var borderColor = Color.FromArgb(
                Properties.Settings.Default.LensBorderColor.A,
                Properties.Settings.Default.LensBorderColor.R,
                Properties.Settings.Default.LensBorderColor.G,
                Properties.Settings.Default.LensBorderColor.B);
            
            LensBorderColor = new SolidColorBrush(borderColor);
            LensBorderWidth = Properties.Settings.Default.LensBorderWidth;
            LensShape = (LensShape)Properties.Settings.Default.LensShape;
            OverlayOpacity = 0;

            EventAggregator.GetEvent<SettingsChangedEvent>().Subscribe(OnSettingsChanged);
        }

        private void OnSettingsChanged(SettingsChangedEventData updatedSettings)
        {
            LensShape = updatedSettings.LensShape;
            LenseRadius = updatedSettings.LensRadius;
            LensBorderWidth = updatedSettings.LensBorderWidth;
            LensOffset = updatedSettings.LensOffset;
        }

        public override void Dispose()
        {
            base.Dispose();
            EventAggregator.GetEvent<SettingsChangedEvent>().Unsubscribe(OnSettingsChanged);
        }
    }
}
