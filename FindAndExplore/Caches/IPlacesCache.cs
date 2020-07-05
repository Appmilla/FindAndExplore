using System;
using DynamicData;
using FindAndExplore.ViewModels;

namespace FindAndExplore.Caches
{
    public interface IPlacesCache
    {
        SourceCache<PlaceViewModel, String> ViewModelCache { get; }
        
        void Clear();
    }
}