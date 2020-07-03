using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DynamicData;
using FindAndExplore.DynamicData;
using FindAndExplore.Extensions;
using FindAndExplore.Infrastructure;
using FindAndExplore.Mapping;
using FindAndExplore.Mapping.Expressions;
using FindAndExplore.Mapping.Layers;
using FindAndExplore.Mapping.Sources;
using FindAndExplore.Queries;
using FindAndExplore.Reactive;
using FoursquareApi.Client;
using Geohash;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.DatasetProviders
{
    public interface IFoursquareDatasetProvider
    {
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        bool IsBusy { get; }

        ReactiveCommand<Position, ICollection<Venue>> Load { get; }
        ReactiveCommand<Position, ICollection<Venue>> Refresh { get; }
        ReactiveCommand<Unit, Unit> CancelInFlightQueries { get; }
        SourceCache<Venue, String> ViewModelCache { get; }
        FeatureCollection Features { get; }
    }

    public class FoursquareDatasetProvider : DatasetProvider<Venue, ICollection<Venue>, string>, IFoursquareDatasetProvider
    {
        private const int Venues_Radius = 5000;
        
        private const string GEOJSON_VENUE_SOURCE_ID = "GEOJSON_VENUE_SOURCE_ID";
        private const string BAR_MARKER_IMAGE_ID = "BAR_MARKER_IMAGE_ID";
        private const string VENUE_MARKER_LAYER_ID = "VENUE_MARKER_LAYER_ID";
        
        readonly IFoursquareQuery _foursquareQuery;
        readonly IMapLayerController _mapLayerController;
        readonly ISchedulerProvider _schedulerProvider;
        readonly IErrorReporter _errorReporter;

        private GeoJsonSource _venuesSource;
        
        Geohasher _geohasher = new Geohasher();
        
        private static readonly Func<Venue, string> VenueKeySelector = venue => venue.Id;
        
        [Reactive]
        public FeatureCollection Features { get; set; }
        
        public FoursquareDatasetProvider(
            IFoursquareQuery foursquareQuery,
            IMapLayerController mapLayerController,
            ISchedulerProvider schedulerProvider,
            IErrorReporter errorReporter)
        {
            _foursquareQuery = foursquareQuery;
            _mapLayerController = mapLayerController;
            _schedulerProvider = schedulerProvider;
            _errorReporter = errorReporter;
            
            ViewModelCache = new SourceCache<Venue, string>(VenueKeySelector);

            Load = ReactiveCommand.CreateFromObservable<Position, ICollection<Venue>>(
                OnLoad,
                this.WhenAnyValue(x => x.IsBusy).Select(x => !x),
                outputScheduler: _schedulerProvider.ThreadPool);
            Load.ThrownExceptions.Subscribe(Venues_OnError);
            Load.Subscribe(LoadVenues_OnNext);
            
            Refresh = ReactiveCommand.CreateFromObservable<Position, ICollection<Venue>>(
                OnRefresh,
                this.WhenAnyValue(x => x.IsBusy).Select(x => !x),
                outputScheduler: _schedulerProvider.ThreadPool);
            Refresh.ThrownExceptions.Subscribe(Venues_OnError);
            Refresh.Subscribe(RefreshVenues_OnNext);
            
            
            CancelInFlightQueries = ReactiveCommand.Create(
                () => { },
                this.WhenAnyObservable(
                    l => l.Load.IsExecuting,
                    r => r.Refresh.IsExecuting,
                    (l, r) => l || r));
        }

        string GetGeoHash(Position centerPosition)
        {
            //https://github.com/postlagerkarte/geohash-dotnet
            //https://www.elastic.co/guide/en/elasticsearch/guide/current/geohashes.html#geohashes

            //precision level 5 is approx 4.9km x 4.9km so as long as the current map centre is within that square then the previously cached data can be used
            //precision level 6 is approx 1.2km x 0.61km 
            var geoHash = _geohasher.Encode(centerPosition.Latitude, centerPosition.Longitude, 6);
            return geoHash;
        }

        string GetCacheKey(Position centerPosition)
        {
            return $"{GetGeoHash(centerPosition)}-find_and_explore/foursquare-venues";
        }
        
        private IObservable<ICollection<Venue>> OnLoad(Position centerPosition)
        {
            return _foursquareQuery
                .GetVenues(centerPosition.Latitude, centerPosition.Longitude, Venues_Radius, GetCacheKey(centerPosition))
                .TakeUntil(CancelInFlightQueries);
        }
        
        private IObservable<ICollection<Venue>> OnRefresh(Position centerPosition)
        {
            return _foursquareQuery
                .RefreshVenues(centerPosition.Latitude, centerPosition.Longitude, Venues_Radius, GetCacheKey(centerPosition))
                .TakeUntil(CancelInFlightQueries);
        }

        private void LoadVenues_OnNext(ICollection<Venue> venues)
        {
            try
            {
                var venuesFeatureCollection = venues.ToFeatureCollection();
                
                _venuesSource = new GeoJsonSource(GEOJSON_VENUE_SOURCE_ID, venuesFeatureCollection);
                
                _schedulerProvider.MainThread.Schedule(_ =>
                {
                    _mapLayerController.AddSource(_venuesSource);
                    
                    SetUpVenuesImage();
                    SetUpVenuesMarkerLayer();
                    
                    Features = venuesFeatureCollection;
                    ViewModelCache.UpdateCache(venues, VenueKeySelector);
                });
            }
            catch (Exception exception)
            {
                Venues_OnError(exception);
            }
        }
        
        private void SetUpVenuesImage()
        {
            _mapLayerController.AddImage(BAR_MARKER_IMAGE_ID, "local_bar");
        }

        private void SetUpVenuesMarkerLayer()
        {
            /*
            _mapLayerController.AddLayer(new SymbolLayer(VENUE_MARKER_LAYER_ID, GEOJSON_VENUE_SOURCE_ID)
            {
                IconImage = BAR_MARKER_IMAGE_ID,
                IconAllowOverlap = true,
                IconIgnorePlacement = true,
                IconOffset = (new float[] { 0f, -8f })
            });
            */
            
            _mapLayerController.AddLayer(new SymbolLayer(VENUE_MARKER_LAYER_ID, GEOJSON_VENUE_SOURCE_ID)
            {
                IconImage = Expression.Literal(BAR_MARKER_IMAGE_ID),
                IconAllowOverlap = Expression.Literal(true),
                IconIgnorePlacement = Expression.Literal(true),
                IconOffset = Expression.Literal(new float[] { 0f, -8f })
            });
        }
        
        private void RefreshVenues_OnNext(ICollection<Venue> venues)
        {
            try
            {
                UpdateVenues(venues);
            }
            catch (Exception exception)
            {
                Venues_OnError(exception);
            }
        }

        void UpdateVenues(ICollection<Venue> venues)
        {
            var venuesFeatureCollection = venues.ToFeatureCollection();
            
            _schedulerProvider.MainThread.Schedule(_ =>
            {
                _mapLayerController.UpdateSource(GEOJSON_VENUE_SOURCE_ID, venuesFeatureCollection);
                
                Features = venuesFeatureCollection;
                ViewModelCache.UpdateCache(venues, VenueKeySelector);
            });
        }

        private void Venues_OnError(Exception obj)
        {            
        }
        
    }
}