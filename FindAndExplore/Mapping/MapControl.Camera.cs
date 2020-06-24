using GeoJSON.Net.Geometry;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.Mapping
{
    public interface IMapCamera
    {
        Position Center { get; set; }

        Position LastKnownUserPosition { get; set; }

        double? ZoomLevel { get; set; }
        
        double Heading { get; set; }
    }
    
    public partial class MapControl : IMapCamera
    {
        [Reactive]
        public Position Center { get; set; }

        [Reactive]
        public Position LastKnownUserPosition { get; set; }

        [Reactive]
        public double? ZoomLevel { get; set; }
        
        [Reactive]
        public double Heading { get; set; }
    }
}