using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.DatasetProviders
{
    //TOOD I expect we will create a view model to represent each point of interest/place on the map
    //We would then map the data returned from the various sources our Mongo/Foursquare/Facebook/Trip Advisor
    //into this view model
    //Dynamic Data supports combining caches so for the list of placs we can combine the cache from each DatasetProvider
    //Currently we will have a layer on the map for each DatasetProvider, they could be turned on or off individually
    //we might combine these into a single layer for places maybe, and another say for Events?
    public interface IDatasetProvider<TViewModel, TModel, TCollection, TKey>
        where TModel : class
        where TViewModel : class, IReactiveObject
    {
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        bool IsBusy { get; }
        
        ReactiveCommand<Position, TCollection> Load { get; }
        ReactiveCommand<Position, TCollection> Refresh { get; }
        ReactiveCommand<Unit, Unit> CancelInFlightQueries { get; }
        SourceCache<TViewModel, TKey> ViewModelCache { get; }
        
        FeatureCollection Features { get; }
    }

    public class DatasetProvider<TViewModel, TModel, TCollection, TKey> : DatasetProvider, IDatasetProvider<TViewModel, TModel, TCollection, TKey>
        where TModel : class
        where TViewModel : class, IReactiveObject
        where TCollection : ICollection<TModel>//, INotifyCollectionChanged
    {
        [ObservableAsProperty]
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public bool IsBusy { get; }
        
        public ReactiveCommand<Position, TCollection> Load { get; protected set; }

        public ReactiveCommand<Position, TCollection> Refresh { get; protected set; }
        public ReactiveCommand<Unit, Unit> CancelInFlightQueries { get; protected set; }
        
        public SourceCache<TViewModel, TKey> ViewModelCache { get; protected set; }
        
        public FeatureCollection Features { get; protected set; }
        
        protected DatasetProvider()
        {
            
        }
    }
    
    public class DatasetProvider : ReactiveObject//, IDatasetProvider
    {
        
    }
}