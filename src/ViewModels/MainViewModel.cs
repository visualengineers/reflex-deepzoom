using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using ReFlex.Apps.DeepZoom.Events;
using ReFlex.Apps.DeepZoom.Events.EventData;
using ReFlex.Apps.DeepZoom.Model;
using ReFlex.Core.Common.Components;
using ReFlex.Core.Common.Util;
using ReFlex.Core.Networking.Interfaces;
using ReFlex.Core.Networking.Util;
using Math = System.Math;

namespace ReFlex.Apps.DeepZoom.ViewModels
{
    public class MainViewModel : BindableBase, IDisposable
    {
        private readonly Size _initWindowSize = new(1920, 1080);
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly ReFlexViewModel _reflexVm;

        private WindowStyle _windowStyle;
        private WindowState _windowState;
        private bool _isTitleBarVisible;
        private Size _windowSize;

        private string _currentMode;
        private bool _isExponentialMapping;
        private bool _isLogarithmicMapping;
        private string _titleSourceUri;
        private bool _isLoadingInProgress;
        private string _loadingStateText;
        private ImageData _currentDataSet;
        private bool _showDebug;
        private bool _showInfo;
        private ImageSource _previewImage;
        private bool _showPreviewImage;

        public WindowStyle WindowStyle
        {
            get => _windowStyle;
            set => SetProperty(ref _windowStyle, value);
        }

        public WindowState WindowState
        {
            get => _windowState;
            set => SetProperty(ref _windowState, value);
        }

        public bool IsTitleBarVisible
        {
            get => _isTitleBarVisible;
            set => SetProperty(ref _isTitleBarVisible, value);
        }

        public double WindowWidth
        {
            get => _windowSize.Width;
            set
            {
                _windowSize.Width = value;
                RaisePropertyChanged(nameof(WindowWidth));
                _eventAggregator?.GetEvent<ContentResizeEvent>().Publish(_windowSize);
            }
        }

        public double WindowHeight
        {
            get => _windowSize.Height;
            set
            {
                _windowSize.Height = value;
                RaisePropertyChanged(nameof(WindowHeight));
                _eventAggregator?.GetEvent<ContentResizeEvent>().Publish(_windowSize);
            }
        }

        public bool IsExponentialMapping
        {
            get => _isExponentialMapping;
            set => SetProperty(ref _isExponentialMapping, value);
        }

        public bool IsLogarithmicMapping
        {
            get => _isLogarithmicMapping;
            set => SetProperty(ref _isLogarithmicMapping, value);
        }

        public string CurrentMode
        {
            get => _currentMode;
            set => SetProperty(ref _currentMode, value);
        }

        public string TitleSourceUri
        {
            get => _titleSourceUri;
            set => SetProperty(ref _titleSourceUri, value);
        }

        public bool IsLoadingInProgress
        {
            get => _isLoadingInProgress;
            private set => SetProperty(ref _isLoadingInProgress, value);
        }

        public string LoadingStateText
        {
            get => _loadingStateText;
            private set => SetProperty(ref _loadingStateText, value);
        }

        public ImageData CurrentDataSet
        {
            get => _currentDataSet;
            set => SetProperty(ref _currentDataSet, value);
        }

        public bool ShowDebug
        {
            get => _showDebug;
            set => SetProperty(ref _showDebug, value);
        }
        
        public bool ShowInfo
        {
            get => _showInfo;
            set => SetProperty(ref _showInfo, value);
        }

        public ImageSource PreviewImage
        {
            get => _previewImage;
            set => SetProperty(ref _previewImage, value);
        }

        public bool ShowPreviewImage
        {
            get => _showPreviewImage;
            set => SetProperty(ref _showPreviewImage, value);
        }

        public ICommand TerminateApplicationCommand { get; }
        public ICommand ToggleFullscreenCommand { get; }
        public ICommand WindowLoadedCommand { get; }

        public ICommand ShowView1Command { get; }
        public ICommand ShowView2Command { get; }
        public ICommand ShowView3Command { get; }
        public ICommand ShowView4Command { get; }
        
        public ICommand NextImageCommand { get; }
        
        public ICommand PreviousImageCommand { get; }
        
        public ICommand ToggleDebugViewCommand { get; }
        
        public ICommand ToggleHelpCommand { get; }

        public ICommand ToggleExponentialMappingCommand { get; }
        public ICommand ToggleLogarithmicMappingCommand { get; }

        public MainViewModel(IRegionManager regionManager, IEventAggregator eventAggregator, ReFlexViewModel reflexVm)
        {
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;
            _reflexVm = reflexVm;
            
            _eventAggregator.GetEvent<DataSetChangedEvent>().Subscribe(OnDataSetChanged);
            _eventAggregator.GetEvent<ImageSourceLoadingFinishedEvent>().Subscribe(OnImageSourceLoadingFinished);
            _eventAggregator.GetEvent<ImageComponentInitializationStateChangedEvent>()
                .Subscribe(OnImageComponentInitializationStateChanged);
            _eventAggregator.GetEvent<PreviewImageLoadedEvent>().Subscribe(OnPreviewImageLoaded);

            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
            IsTitleBarVisible = true;
            WindowWidth = _initWindowSize.Width;
            WindowHeight = _initWindowSize.Height;

            TerminateApplicationCommand = new DelegateCommand(TerminateApplication);
            ToggleFullscreenCommand = new DelegateCommand(ToggleFullscreen);
            WindowLoadedCommand = new DelegateCommand(OnWindowLoaded);
            
            _reflexVm.Connect();
            _reflexVm.DataReceived += DataReceived;

            ShowView1Command = new DelegateCommand(() => ChangeView("DirectPanningWithLenseView"));
            ShowView2Command = new DelegateCommand(() => ChangeView("DirectPanningWithoutLenseView"));
            ShowView3Command = new DelegateCommand(() => ChangeView("JoystickBasedPanningWithLenseView"));
            ShowView4Command = new DelegateCommand(() => ChangeView("JoystickBasedPanningWithoutLenseView"));

            NextImageCommand = new DelegateCommand(() => RequestImageChange(ImageChangeRequestType.NextImage));
            PreviousImageCommand = new DelegateCommand(() => RequestImageChange(ImageChangeRequestType.PreviousImage));

            ToggleExponentialMappingCommand = new DelegateCommand(() => IsExponentialMapping = !IsExponentialMapping);
            ToggleLogarithmicMappingCommand = new DelegateCommand(() => IsLogarithmicMapping = !IsLogarithmicMapping);

            ToggleDebugViewCommand = new DelegateCommand(() => ShowDebug = !ShowDebug);
            ToggleHelpCommand = new DelegateCommand(() => ShowInfo = !ShowInfo);

            TitleSourceUri = "/Resources/deepzoom_title.png";
        }

        public void Dispose()
        {
            _reflexVm.DataReceived -= DataReceived;
            _reflexVm.Dispose();
            
            _eventAggregator.GetEvent<DataSetChangedEvent>().Subscribe(OnDataSetChanged);
            _eventAggregator.GetEvent<ImageSourceLoadingFinishedEvent>().Unsubscribe(OnImageSourceLoadingFinished);
            _eventAggregator.GetEvent<ImageComponentInitializationStateChangedEvent>()
                .Unsubscribe(OnImageComponentInitializationStateChanged);
            _eventAggregator.GetEvent<PreviewImageLoadedEvent>().Unsubscribe(OnPreviewImageLoaded);
        }

        private static void TerminateApplication() => Application.Current.Shutdown();

        private void ToggleFullscreen() => ChangeWindowState(!IsFullscreen);

        private void ChangeWindowState(bool toFullscreen = true)
        {
            if (toFullscreen)
            {
                WindowState = WindowState.Maximized;
                WindowStyle = WindowStyle.None;
                IsTitleBarVisible = false;
            }
            else
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.SingleBorderWindow;
                IsTitleBarVisible = true;
                WindowWidth = _initWindowSize.Width;
                WindowHeight = _initWindowSize.Height;
            }
        }

        private bool IsFullscreen => WindowState == WindowState.Maximized && WindowStyle == WindowStyle.None;

        private void ChangeView(string viewName)
        {
            _eventAggregator.GetEvent<ViewChangedEvent>().Publish(viewName);
            _regionManager.RequestNavigate("ContentRegion", viewName);
            PreviewImage = null;
            ShowPreviewImage = false;
            // IsLoadingInProgress = true;
        }

        private void RequestImageChange(ImageChangeRequestType type)
        {
            var args = new ImageChangeRequestArguments { Type = type };
            _eventAggregator.GetEvent<RequestImageChangeEvent>().Publish(args);
            // PreviewImage = null;
            // ShowPreviewImage = false;
        }
        
        private void OnWindowLoaded()
        {
            ChangeView("DirectPanningWithLenseView");
            ChangeWindowState();
        }

        private void DataReceived(object sender, NetworkingDataMessage message)
        {
            if (message?.Message == null)
                return;

            var interactions = SerializationUtils.DeserializeFromJson<List<Interaction>>(message.Message);

            var interaction = interactions.Count > 0  
                ? interactions.OrderBy(i => i.Position.Z).First()
                : new Interaction(new Point3(0f, 0f, 0f), InteractionType.None, 0);

            interaction.Position.Z = -interaction.Position.Z;

            if (interaction.Position.Z < 0)
                interaction.Position.Z = 0;

            if (IsExponentialMapping)
                interaction.Position.Z = ExponentialMapping(interaction.Position.Z);

            if (IsLogarithmicMapping)
                interaction.Position.Z = LogarithmicMapping(interaction.Position.Z);

            _eventAggregator.GetEvent<InteractionReceivedEvent>().Publish(interaction);
        }
        
        private void OnDataSetChanged(ImageData dataset) => CurrentDataSet = dataset;
        
        private void OnImageSourceLoadingFinished(string imagePath) => LoadingStateText = $"Loaded {imagePath}...";

        private void OnImageComponentInitializationStateChanged(bool loadingFinished) => IsLoadingInProgress = !loadingFinished;
        
        private void OnPreviewImageLoaded(ImageSource previewImage)
        {
            PreviewImage = previewImage;
            ShowPreviewImage = previewImage != null;
        }
        
        private static float ExponentialMapping(float input) =>
            (float)((Math.Exp(input) - 1) / (Math.E - 1));

        private static float LogarithmicMapping(float input) =>
            (float)(Math.Log(input + 1, Math.E) / Math.Log(2, Math.E));
    }
}
