using System;
using System.Threading;
using System.Windows;
using Prism.Events;
using Prism.Ioc;
using ReFlex.Apps.DeepZoom.Diagnostics;
using ReFlex.Apps.DeepZoom.Model;
using ReFlex.Apps.DeepZoom.ViewModels;
using ReFlex.Apps.DeepZoom.Views;
using ReFlex.Core.Networking.Components;
using ReFlex.Core.Networking.Interfaces;
using Websocket.Client;

namespace ReFlex.Apps.DeepZoom
{
    public partial class App
    {
        private DiagnosticsService _diagnosticsService;
        
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(typeof(DataRepository), new DataRepository());
            containerRegistry.Register<ReFlexViewModel>();
            
            containerRegistry.RegisterInstance(typeof(DiagnosticsClient), new DiagnosticsClient());
            
            containerRegistry.RegisterForNavigation<DirectPanningWithLenseView>();
            containerRegistry.RegisterForNavigation<DirectPanningWithoutLenseView>();
            containerRegistry.RegisterForNavigation<JoystickBasedPanningWithLenseView>();
            containerRegistry.RegisterForNavigation<JoystickBasedPanningWithoutLenseView>();
        }
        
        protected override Window CreateShell()
        {
            var evtAggregator = ContainerLocator.Current.Resolve<IEventAggregator>();
            var client = ContainerLocator.Current.Resolve<DiagnosticsClient>();
                     
            _diagnosticsService = new DiagnosticsService(client, evtAggregator);
            return ContainerLocator.Container.Resolve<MainView>();
        }
    }
}
