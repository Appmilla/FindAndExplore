using ReactiveUI;

namespace FindAndExplore.Mapping
{
    public interface IMapControl : IMapCamera, IMapDelegate
    {
        
    }
    
    public partial class MapControl : ReactiveObject, IMapControl
    {
        
    }
}