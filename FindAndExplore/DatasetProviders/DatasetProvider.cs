using System.Collections.Generic;
using System.Reactive;
using DynamicData;
using Geohash;
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
        SourceCache<TViewModel, TKey> ViewModelCache { get; set; }
        
        FeatureCollection Features { get; }
    }

    public class DatasetProvider<TViewModel, TModel, TCollection, TKey> : DatasetProvider, IDatasetProvider<TViewModel, TModel, TCollection, TKey>
        where TModel : class
        where TViewModel : class, IReactiveObject
        where TCollection : ICollection<TModel>
    {
        protected Geohasher _geohasher = new Geohasher();
        
        [ObservableAsProperty]
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public bool IsBusy { get; }
        
        public ReactiveCommand<Position, TCollection> Load { get; protected set; }

        public ReactiveCommand<Position, TCollection> Refresh { get; protected set; }
        public ReactiveCommand<Unit, Unit> CancelInFlightQueries { get; protected set; }
        
        //The SourceCache is shared and is set by the MapViewModel currently
        public SourceCache<TViewModel, TKey> ViewModelCache { get; set; }
        
        public FeatureCollection Features { get; protected set; }
        
        protected DatasetProvider()
        {
            
        }
        
        protected string GetGeoHash(Position centerPosition, int precision)
        {
            //https://github.com/postlagerkarte/geohash-dotnet
            //https://www.elastic.co/guide/en/elasticsearch/guide/current/geohashes.html#geohashes

            //precision level 5 is approx 4.9km x 4.9km so as long as the current map centre is within that square then the previously cached data can be used
            //precision level 6 is approx 1.2km x 0.61km 
            var geoHash = _geohasher.Encode(centerPosition.Latitude, centerPosition.Longitude, precision);
            return geoHash;
        }
    }
    
    public class DatasetProvider : ReactiveObject
    {
        
    }
}