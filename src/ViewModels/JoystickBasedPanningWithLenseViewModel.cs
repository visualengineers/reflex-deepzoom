using System.IO;
using System.Timers;
using System.Windows;
using Prism.Events;
using ReFlex.Apps.DeepZoom.Model;
using ReFlex.Apps.DeepZoom.Util;
using ReFlex.Core.Common.Components;
using ReFlex.Core.Common.Util;
using Math = System.Math;

namespace ReFlex.Apps.DeepZoom.ViewModels
{
    public class JoystickBasedPanningWithLenseViewModel : ZoomImageViewModelBase
    {
        private readonly float _motionThreshold;
        private readonly int _secondsUntilPanMode;

        private Point _zoomCenter;
        private Point _lenseCenter;
        private Point _joystickCenter;
        private double _lenseRadius;
        private double _scale;
        private bool _isLenseVisible;
        private Interaction _lastInteraction;
        private Vector2 _momentumZ;
        private Vector2 _momentumXY;
        private MotionMode _currentMode;
        private Timer _timer;

        public override Point ZoomCenter
        {
            get => _zoomCenter;
            set
            {
                SetProperty(ref _zoomCenter, value);
                RaisePropertyChanged(nameof(LenseCenter));
            }
        }

        public Point LenseCenter
        {
            get => CurrentMode == MotionMode.Pan ? _joystickCenter : _zoomCenter;
            set => SetProperty(ref _lenseCenter, value);
        }

        public double LenseRadius
        {
            get => _lenseRadius;
            set => SetProperty(ref _lenseRadius, value);
        }

        public bool IsLenseVisible
        {
            get => _isLenseVisible;
            set => SetProperty(ref _isLenseVisible, value);
        }

        public override double Scale
        {
            get => _scale;
            set
            {
                SetProperty(ref _scale, value);
                IsLenseVisible = _scale > 0;
            }
        }

        public override Interaction LastInteraction
        {
            get => _lastInteraction;
            set
            {
                if (_lastInteraction != null)
                {
                    // Bewegungsvektor berechnen
                    MomentumZ = new Vector2(_lastInteraction.Position.X - value.Position.X,
                        _lastInteraction.Position.Y - value.Position.Y);

                    // Wenn Interaktionstiefe unter minimalen Threshold -> Idle
                    CurrentMode = _lastInteraction.Type == InteractionType.None
                        ? MotionMode.Idle
                        : CurrentMode;

                    if (CurrentMode == MotionMode.Idle)
                        Scale = 1;

                    // Wenn Interaktionstiefe über minimalen Threshold und kein Panmodus -> Zoom
                    CurrentMode = CurrentMode != MotionMode.Pan && _lastInteraction.Type != InteractionType.None
                        ? MotionMode.Zoom
                        : CurrentMode;

                    // Timer starten wenn Bewegung kleiner als Threshold
                    // Wenn Timer abgelaufen ist -> Pan
                    if (_timer == null &&
                        CurrentMode == MotionMode.Zoom &&
                        Math.Abs(_lastInteraction.Position.Z - value.Position.Z) < _motionThreshold)
                        StartTimer(out _timer, _secondsUntilPanMode);

                    else if (_timer != null &&
                             Math.Abs(_lastInteraction.Position.Z - value.Position.Z) > _motionThreshold)
                        DisposeTimer(ref _timer);
                }

                // neuen Wert setzen
                SetProperty(ref _lastInteraction, value);

                // Was passiert in den Modi
                switch (CurrentMode)
                {
                    // Wenn Idle oder Pan, kann Cursor in X oder Y Richtung wandern
                    case MotionMode.Idle:
                        ZoomCenter = new Point(
                            _lastInteraction.Position.X * UserControlWidth,
                            _lastInteraction.Position.Y * UserControlHeight
                        );
                        break;

                    case MotionMode.Pan: //when MomentumXY.Length > MotionThreshold:

                        MomentumXY = new Vector2((float)_joystickCenter.X - value.Position.X * 1920,
                            (float)_joystickCenter.Y - value.Position.Y * 1080);

                        ZoomCenter = new Point(
                            ZoomCenter.X + MomentumXY.X / 100,
                            ZoomCenter.Y + MomentumXY.Y / 100
                        );
                        break;

                    // Wenn Zoom, kann Cursor in Z Richtung wandern
                    case MotionMode.Zoom:
                        Scale = 1 + _lastInteraction.Position.Z * EffectiveZoomRange;
                        break;
                }
            }
        }

        public Vector2 MomentumXY
        {
            get => _momentumXY;
            set => SetProperty(ref _momentumXY, value);
        }

        public Vector2 MomentumZ
        {
            get => _momentumZ;
            set => SetProperty(ref _momentumZ, value);
        }

        public MotionMode CurrentMode
        {
            get => _currentMode;
            set => SetProperty(ref _currentMode, value);
        }

        public JoystickBasedPanningWithLenseViewModel(IEventAggregator eventAggregator, DataRepository repository)
            : base(eventAggregator, repository)
        {
            ScaleMultiplier = Properties.Settings.Default.ScaleMultiplicator;
            _motionThreshold = Properties.Settings.Default.MotionThreshold;
            _secondsUntilPanMode = Properties.Settings.Default.SecondsUntilPanMode;
            LenseRadius = Properties.Settings.Default.LensRadius;          
            UserControlWidth = 1920;
            UserControlHeight = 1080;
        }

        private void StartPanMode(object sender, ElapsedEventArgs args)
        {
            CurrentMode = MotionMode.Pan;
            _joystickCenter = new Point(ZoomCenter.X, ZoomCenter.Y);
            DisposeTimer(ref _timer);
        }

        private void StartTimer(out Timer timer, int secs)
        {
            timer = new Timer();
            timer.Elapsed += StartPanMode;
            timer.Interval = secs * 1000;
            timer.Start();
        }

        private void DisposeTimer(ref Timer timer)
        {
            timer.Elapsed -= StartPanMode;
            timer.Stop();
            timer.Close();
            timer = null;
        }
    }
}
