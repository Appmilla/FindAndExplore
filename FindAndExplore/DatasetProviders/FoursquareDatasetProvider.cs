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
using FindAndExplore.ViewModels;
using FoursquareApi.Client;
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
        SourceCache<PlaceViewModel, String> ViewModelCache { get; }
        FeatureCollection Features { get; }
    }

    public class FoursquareDatasetProvider : DatasetProvider<PlaceViewModel, Venue, ICollection<Venue>, string>, IFoursquareDatasetProvider
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
       
        private static readonly Func<PlaceViewModel, string> PlacesKeySelector = place => place.Id;
        
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
          
            ViewModelCache = new SourceCache<PlaceViewModel, string>(PlacesKeySelector);
            
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

        string GetCacheKey(Position centerPosition)
        {
            return $"{GetGeoHash(centerPosition, 6)}-find_and_explore/foursquare-venues";
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
                    var places = venues.ToPlaceCollection();
                    
                    //using Edit locks the Cache so the operations within it are threadsafe
                    ViewModelCache.Edit(innerCache =>
                    {
                        ViewModelCache.Clear();
                        ViewModelCache.AddOrUpdate(places);
                    });
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
                var places = venues.ToPlaceCollection();
                
                //using Edit locks the Cache so the operations within it are threadsafe
                ViewModelCache.Edit(innerCache =>
                {
                    ViewModelCache.Clear();
                    ViewModelCache.AddOrUpdate(places);
                });
            });
        }

        private void Venues_OnError(Exception exception)
        {    
            _errorReporter.TrackError(exception);
        }
        
    }
}