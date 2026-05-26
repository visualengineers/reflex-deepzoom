using System.IO;
using System.Windows;
using Prism.Events;
using ReFlex.Apps.DeepZoom.Model;
using ReFlex.Core.Common.Components;

namespace ReFlex.Apps.DeepZoom.ViewModels
{
    public class DirectPanningWithoutLenseViewModel : ZoomImageViewModelBase
    {
        private Interaction _lastInteraction;
        
        public override Interaction LastInteraction
        {
            get => _lastInteraction;
            set
            {
                SetProperty(ref _lastInteraction, value);

                ZoomCenter = new Point(
                    _lastInteraction.Position.X * UserControlWidth,
                    _lastInteraction.Position.Y * UserControlHeight
                );

                Scale = 1 + _lastInteraction.Position.Z * EffectiveZoomRange;
                
                OverlayOpacity = _lastInteraction.Position.Z * 2.0;
                RaisePropertyChanged(nameof(OverlayOpacity));
            }
        }
        
        public DirectPanningWithoutLenseViewModel(IEventAggregator eventAggregator, DataRepository repository) 
            : base(eventAggregator, repository)
        {
            ScaleMultiplier = Properties.Settings.Default.ScaleMultiplicator;          
            OverlayOpacity = 0;
        }
    }
}
