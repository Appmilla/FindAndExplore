using System.Reactive;
using GeoJSON.Net.Geometry;
using ReactiveUI;

namespace FindAndExplore.Mapping
{
    public interface IMapDelegate
    {
        ReactiveCommand<Unit, Unit> StyleImageMissing { get; set; }
        
        ReactiveCommand<MapStyle, Unit> DidFinishLoadingStyle { get; set; }  
        
        ReactiveCommand<Unit, Unit> DidFinishLoading { get; set; }
        
        ReactiveCommand<Position, Unit> DidTapOnMap { get; set; }
        
        ReactiveCommand<Unit, Unit> DidTapOnCalloutView { get; set; }
        
        ReactiveCommand<Unit, Unit> DidTapOnMarker { get; set; }
        
        ReactiveCommand<Unit, Unit> DidBoundariesChanged { get; set; }
    }
    
    public partial class MapControl : IMapDelegate
    {
        public ReactiveCommand<Unit, Unit> StyleImageMissing { get; set; }
        
        public ReactiveCommand<MapStyle, Unit> DidFinishLoadingStyle { get; set; }
        
        public ReactiveCommand<Unit, Unit> DidFinishLoading { get; set; }
        

        public ReactiveCommand<Position, Unit> DidTapOnMap { get; set; }
        
        
        public ReactiveCommand<Unit, Unit> DidTapOnCalloutView { get; set; }
        
        
        public ReactiveCommand<Unit, Unit> DidTapOnMarker { get; set; }
        
        
        public ReactiveCommand<Unit, Unit> DidBoundariesChanged { get; set; }
        
        
    }
}