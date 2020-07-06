using System;
using DynamicData;
using FindAndExplore.DatasetProviders;
using FindAndExplore.Infrastructure;
using FindAndExplore.Reactive;
using FindAndExplore.ViewModels;
using ReactiveUI;
using System.Reactive.Concurrency;

namespace FindAndExplore.Caches
{
    public class PlacesCache : ReactiveObject, IPlacesCache
    {
        readonly ISchedulerProvider _schedulerProvider;
        readonly IErrorReporter _errorReporter;
        readonly IFoursquareDatasetProvider _foursquareDatasetProvider;
        readonly IFindAndExploreDatasetProvider _findAndExploreDatasetProvider;
        readonly IFacebookDatasetProvider _facebookDatasetProvider;

        public SourceCache<PlaceViewModel, String> ViewModelCache { get; }
        
        public void Clear()
        {
            _schedulerProvider.MainThread.Schedule(() =>
            {
                ViewModelCache.Clear();
            });
        }

        static readonly Func<PlaceViewModel, string> PlacesKeySelector = place => place.Id;
        
        public PlacesCache(
            ISchedulerProvider schedulerProvider,
            IErrorReporter errorReporter,
            IFoursquareDatasetProvider foursquareDatasetProvider,
            IFindAndExploreDatasetProvider findAndExploreDatasetProvider,
            IFacebookDatasetProvider facebookDatasetProvider)
        {
            _schedulerProvider = schedulerProvider;
            _errorReporter = errorReporter;
            _foursquareDatasetProvider = foursquareDatasetProvider;
            _findAndExploreDatasetProvider = findAndExploreDatasetProvider;
            _facebookDatasetProvider = facebookDatasetProvider;

            ViewModelCache = new SourceCache<PlaceViewModel, string>(PlacesKeySelector);
            
            // connect the various DatasetProvider ViewModelCaches to this combined cache
            _ = _findAndExploreDatasetProvider.ViewModelCache.Connect().PopulateInto(ViewModelCache);
            _ = _foursquareDatasetProvider.ViewModelCache.Connect().PopulateInto(ViewModelCache);
            _ = _facebookDatasetProvider.ViewModelCache.Connect().PopulateInto(ViewModelCache);
        }
    }
}