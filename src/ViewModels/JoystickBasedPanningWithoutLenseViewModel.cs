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
    public class JoystickBasedPanningWithoutLenseViewModel : ZoomImageViewModelBase
    {
        private readonly float _motionThresholdZ;
        private readonly float _acceleration;
        private readonly float _motionThresholdXy;
        private readonly int _secondsUntilPanMode;

        private Point _joystickCenter;
        private Point _joystickMarker;
        private Interaction _lastInteraction;
        private Vector2 _momentumZ;
        private Vector2 _momentumXY;
        private MotionMode _currentMode;
        private Timer _timer;

        public override Point ZoomCenter
        {
            get => ZoomCenterValue;
            set
            {
                SetProperty(ref ZoomCenterValue, value);
                if (CurrentMode < MotionMode.Pan)
                    JoystickMarker = new Point(ZoomCenter.X - 1920.0 / 2.0, ZoomCenter.Y - 1080.0 / 2.0);
            }
        }

        public Point JoystickMarker
        {
            get => _joystickMarker;
            set => SetProperty(ref _joystickMarker, value);
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
                        Math.Abs(_lastInteraction.Position.Z - value.Position.Z) < _motionThresholdZ)
                        StartTimer(out _timer, _secondsUntilPanMode);

                    else if (_timer != null &&
                        Math.Abs(_lastInteraction.Position.Z - value.Position.Z) > _motionThresholdZ)
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

                        MomentumXY = new Vector2(
                            (float)_joystickCenter.X - value.Position.X * 1920,
                            (float)_joystickCenter.Y - value.Position.Y * 1080);

                        if (MomentumXY.Length > _motionThresholdXy)
                            ZoomCenter = new Point(
                                ZoomCenter.X + MomentumXY.X / _acceleration,
                                ZoomCenter.Y + MomentumXY.Y / _acceleration
                            );
                        break;

                    // Wenn Zoom, kann Cursor in Z Richtung wandern
                    case MotionMode.Zoom:
                        Scale = 1 + _lastInteraction.Position.Z * EffectiveZoomRange;
                        break;
                }
            }
        }

        public Vector2 MomentumZ
        {
            get => _momentumZ;
            set => SetProperty(ref _momentumZ, value);
        }

        public Vector2 MomentumXY
        {
            get => _momentumXY;
            set => SetProperty(ref _momentumXY, value);
        }

        public MotionMode CurrentMode
        {
            get => _currentMode;
            set
            {
                SetProperty(ref _currentMode, value);
                RaisePropertyChanged(nameof(IsZoomOrPanMode));
            }
        }

        public bool IsZoomOrPanMode => CurrentMode >= MotionMode.Pan;

        public JoystickBasedPanningWithoutLenseViewModel(IEventAggregator eventAggregator, DataRepository repository) 
            : base(eventAggregator, repository)
        {
            ScaleMultiplier = Properties.Settings.Default.ScaleMultiplicator;
            _motionThresholdXy = Properties.Settings.Default.MotionThresholdXY;
            _motionThresholdZ = Properties.Settings.Default.MotionThresholdZ;
            _acceleration = Properties.Settings.Default.Acceleration;
            _secondsUntilPanMode = Properties.Settings.Default.SecondsUntilPanMode;
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
