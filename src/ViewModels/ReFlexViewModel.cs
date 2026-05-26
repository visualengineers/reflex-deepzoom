using System;
using System.Net.WebSockets;
using System.Threading;
using Prism.Events;
using Prism.Mvvm;
using ReFlex.Apps.DeepZoom.Events;
using ReFlex.Apps.DeepZoom.Events.EventData;
using ReFlex.Core.Networking.Util;
using Websocket.Client;
using TimeSpan = System.TimeSpan;
using Uri = System.Uri;

namespace ReFlex.Apps.DeepZoom.ViewModels;

public class ReFlexViewModel: BindableBase, IDisposable
{
    private readonly IEventAggregator _eventAggregator;

    private readonly WebsocketClient _client;
    private bool _isConnected;
    private int _frameNumber;

    public string Address { get; private init; }

    public Guid Id { get; private set; }

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            NotifyConnectionState();
            SetProperty(ref _isConnected, value);
        }
    }

    public int FrameNumber
    {
        get => _frameNumber;
        private set => _frameNumber = value;
    }

    public string StateMsg { get; private set; }

    public event EventHandler<NetworkingDataMessage> DataReceived; 
    
    public ReFlexViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        Id = new Guid();
        
        Address =
            $"{Properties.Settings.Default.Address}:{Properties.Settings.Default.Port}/{Properties.Settings.Default.EndPoint}";
            
        var exitEvent = new ManualResetEvent(false);

        _client = new WebsocketClient(new Uri(Address));

        _client.ErrorReconnectTimeout = TimeSpan.FromSeconds(10);
        _client.LostReconnectTimeout = TimeSpan.FromSeconds(3);
        _client.ReconnectTimeout = TimeSpan.FromSeconds(3);
        _client.ReconnectionHappened.Subscribe(status =>
        {
            _frameNumber = 0;
            IsConnected = true;
            StateMsg = $"RECONNECT | Type: {status.Type}";
        });

        _client.DisconnectionHappened.Subscribe(status => {
            IsConnected = false;
            StateMsg =
                $"DISCONNECTED | Type: {status.Type}, Cancel Reconnect: {status.CancelReconnection}, Exception: {status.Exception}, CloseStatus: {status.CloseStatus}";
        });

        _client.MessageReceived.Subscribe(msg =>
        {
            var args = new NetworkingDataMessage(msg.Text ?? "", Id);
            DataReceived?.Invoke(this, args);
            IsConnected = true;
            _frameNumber++;
            StateMsg = "MessageReceived";
        });
    }

    public void Connect()
    {
        _client.Start();
    }

    public void Dispose()
    {
        _client?.Stop(WebSocketCloseStatus.NormalClosure, "Disconnect");
        _client?.Dispose();
    }

    private void NotifyConnectionState()
    {
        var args = new ConnectionStateEventData
        {
            IsConnected = IsConnected,
            Address = Address,
            Id = Id,
            StateMsg = StateMsg,
            Frame = FrameNumber
        };
        
        _eventAggregator.GetEvent<ConnectionStateChangedEvent>().Publish(args);
    }
}