using System;
using System.Collections.Generic;
using System.Windows;
using Prism.Events;
using Prism.Mvvm;
using ReFlex.Apps.DeepZoom.Events;
using ReFlex.Apps.DeepZoom.Events.EventData;
using ReFlex.Apps.DeepZoom.Model;
using ReFlex.Core.Common.Components;

namespace ReFlex.Apps.DeepZoom.ViewModels;

public class DebugViewModel: BindableBase, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private Interaction _currentInteraction;
    private bool _isConnected;
    private ConnectionStateEventData _connectionState;
    private string _viewName;
    private ImageData _currentDataSet;
    private bool _isLoadingFinished;
    private string _loadedImages;
    private double _lensRadius;
    private double _lensBorderWidth;
    private double _lensOffsetX;
    private double _lensOffsetY;
    private LensShape _lensShape;
    private int _selectedLensShape;

    public Interaction CurrentInteraction
    {
        get => _currentInteraction;
        private set => SetProperty(ref _currentInteraction, value);
    }
    
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            SetProperty(ref _isConnected, value);
            RaisePropertyChanged(nameof(ConnectionStateMessage));
        }
    }

    public ConnectionStateEventData ConnectionState
    {
        get => _connectionState;
        private set
        {
            SetProperty(ref _connectionState, value);
            IsConnected = value.IsConnected;
        }
    }
    
    public string ViewName
    {
        get => _viewName;
        private set => SetProperty(ref _viewName, value);
    }

    public ImageData CurrentDataSet
    {
        get => _currentDataSet;
        private set => SetProperty(ref _currentDataSet, value);
    }

    public bool IsLoadingFinished
    {
        get => _isLoadingFinished;
        private set => SetProperty(ref _isLoadingFinished, value);
    }

    public string LoadedImages
    {
        get => _loadedImages;
        private set => SetProperty(ref _loadedImages, value);
    }

    public Double LensRadius
    {
        get => _lensRadius;
        set
        {
            SetProperty(ref _lensRadius, value);
            OnSettingsUpdated();
        }
    }

    public Double LensBorderWidth
    {
        get => _lensBorderWidth;
        set
        {
            SetProperty(ref _lensBorderWidth, value);
            OnSettingsUpdated();
        }
    }

    public Double LensOffsetX
    {
        get => _lensOffsetX;
        set
        {
            SetProperty(ref _lensOffsetX, value);
            OnSettingsUpdated();
        }
    }

    public Double LensOffsetY
    {
        get => _lensOffsetY;
        set
        {
            SetProperty(ref _lensOffsetY, value);
            OnSettingsUpdated();
        }
    }

    public LensShape LensShape
    {
        get => _lensShape;
        set
        {
            SetProperty(ref _lensShape, value);
            OnSettingsUpdated();
        }
    }
    
    public List<String> LensShapes { get; } = new()
    {
        "Circle",
        "Rectangle"
    };

    

    public int SelectedLensShape
    {
        get => _selectedLensShape;
        set
        {
            SetProperty(ref _selectedLensShape, value);
            LensShape = (LensShape)value;
        }
    }

    public string ConnectionStateMessage => _isConnected ? "Connected" : "Not Connected";
    
    public DebugViewModel(IEventAggregator eventAggregator)
    {
        _lensRadius = Properties.Settings.Default.LensRadius;
        _lensBorderWidth = Properties.Settings.Default.LensBorderWidth;
        _lensOffsetX = Properties.Settings.Default.LensOffset.Width;
        _lensOffsetY = Properties.Settings.Default.LensOffset.Height;
        _selectedLensShape = Properties.Settings.Default.LensShape;
        
        _eventAggregator = eventAggregator;
        _eventAggregator.GetEvent<InteractionReceivedEvent>().Subscribe(OnInteractionReceived);
        _eventAggregator.GetEvent<ConnectionStateChangedEvent>().Subscribe(OnConnectionStateChanged);
        _eventAggregator.GetEvent<ViewChangedEvent>().Subscribe(OnViewChanged);
        _eventAggregator.GetEvent<DataSetChangedEvent>().Subscribe(OnDataSetChanged);
        _eventAggregator.GetEvent<ImageComponentInitializationStateChangedEvent>().Subscribe(OnImageComponentInitializationFinished);
        _eventAggregator.GetEvent<ImageSourceLoadingFinishedEvent>().Subscribe(OnImageSourceLoadingFinished);
    }

    public void Dispose()
    {
        _eventAggregator.GetEvent<InteractionReceivedEvent>().Unsubscribe(OnInteractionReceived);
        _eventAggregator.GetEvent<ConnectionStateChangedEvent>().Unsubscribe(OnConnectionStateChanged);
        _eventAggregator.GetEvent<ViewChangedEvent>().Unsubscribe(OnViewChanged);
        _eventAggregator.GetEvent<DataSetChangedEvent>().Unsubscribe(OnDataSetChanged);
        _eventAggregator.GetEvent<ImageComponentInitializationStateChangedEvent>().Unsubscribe(OnImageComponentInitializationFinished);
        _eventAggregator.GetEvent<ImageSourceLoadingFinishedEvent>().Unsubscribe(OnImageSourceLoadingFinished);
    }

    private void OnInteractionReceived(Interaction interaction) =>  CurrentInteraction = interaction;

    private void OnConnectionStateChanged(ConnectionStateEventData data) => ConnectionState = data;

    private void OnViewChanged(string viewName) => ViewName = viewName;

    private void OnImageSourceLoadingFinished(string imageSourcePath) =>
        LoadedImages += imageSourcePath + Environment.NewLine;

    private void OnImageComponentInitializationFinished(bool loadingFinished) => IsLoadingFinished = loadingFinished;

    private void OnDataSetChanged(ImageData data)
    {
        LoadedImages = "";
        CurrentDataSet = data;
    }

    private void OnSettingsUpdated()
    {
        var data = new SettingsChangedEventData
        {
            LensShape = _lensShape,
            LensOffset = new Size(_lensOffsetX, _lensOffsetY),
            LensRadius = _lensRadius,
            LensBorderWidth = _lensBorderWidth
        };
        _eventAggregator?.GetEvent<SettingsChangedEvent>().Publish(data);
    }
}