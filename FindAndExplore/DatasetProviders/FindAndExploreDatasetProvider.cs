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
using FindAndExploreApi.Client;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.DatasetProviders
{
    public interface IFindAndExploreDatasetProvider
    {
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        bool IsBusy { get; }

        ReactiveCommand<Position, ICollection<PointOfInterest>> Load { get; }
        ReactiveCommand<Position, ICollection<PointOfInterest>> Refresh { get; }
        ReactiveCommand<Unit, Unit> CancelInFlightQueries { get; }
        SourceCache<PlaceViewModel, String> ViewModelCache { get; }
        FeatureCollection Features { get; }
    }

    public class FindAndExploreDatasetProvider : DatasetProvider<PlaceViewModel, PointOfInterest, ICollection<PointOfInterest>, string>, IFindAndExploreDatasetProvider
    {
        private const string GEOJSON_POI_SOURCE_ID = "GEOJSON_POI_SOURCE_ID";
        private const string RED_MARKER_IMAGE_ID = "RED_MARKER_IMAGE_ID";
        private const string POI_MARKER_LAYER_ID = "POI_MARKER_LAYER_ID";
        
        readonly IFindAndExploreQuery _findAndExploreQuery;
        readonly IMapLayerController _mapLayerController;
        readonly ISchedulerProvider _schedulerProvider;
        readonly IErrorReporter _errorReporter;

        private GeoJsonSource _pointOfInterestSource;
       
        private static readonly Func<PlaceViewModel, string> PlacesKeySelector = place => place.Id;
        
        [Reactive]
        public FeatureCollection Features { get; set; }
        
        public FindAndExploreDatasetProvider(
            IFindAndExploreQuery findAndExploreQuery,
            IMapLayerController mapLayerController,
            ISchedulerProvider schedulerProvider,
            IErrorReporter errorReporter)
        {
            _findAndExploreQuery = findAndExploreQuery;
            _mapLayerController = mapLayerController;
            _schedulerProvider = schedulerProvider;
            _errorReporter = errorReporter;
           
            ViewModelCache = new SourceCache<PlaceViewModel, string>(PlacesKeySelector);
            
            Load = ReactiveCommand.CreateFromObservable<Position, ICollection<PointOfInterest>>(
                OnLoad,
                this.WhenAnyValue(x => x.IsBusy).Select(x => !x),
                outputScheduler: _schedulerProvider.ThreadPool);
            Load.ThrownExceptions.Subscribe(PointsOfInterest_OnError);
            Load.Subscribe(LoadVenues_OnNext);
            
            Refresh = ReactiveCommand.CreateFromObservable<Position, ICollection<PointOfInterest>>(
                OnRefresh,
                this.WhenAnyValue(x => x.IsBusy).Select(x => !x),
                outputScheduler: _schedulerProvider.ThreadPool);
            Refresh.ThrownExceptions.Subscribe(PointsOfInterest_OnError);
            Refresh.Subscribe(RefreshPointsOfInterest_OnNext);
            
            CancelInFlightQueries = ReactiveCommand.Create(
                () => { },
                this.WhenAnyObservable(
                    l => l.Load.IsExecuting,
                    r => r.Refresh.IsExecuting,
                    (l, r) => l || r));
        }

        private IObservable<ICollection<PointOfInterest>> OnLoad(Position centerPosition)
        {
            return _findAndExploreQuery
                .GetPointsOfInterest(centerPosition, GetCacheKey(centerPosition))
                .TakeUntil(CancelInFlightQueries);
        }
        
        private IObservable<ICollection<PointOfInterest>> OnRefresh(Position centerPosition)
        {
            return _findAndExploreQuery
                .RefreshPointsOfInterest(centerPosition, GetCacheKey(centerPosition))
                .TakeUntil(CancelInFlightQueries);
        }

        string GetCacheKey(Position centerPosition)
        {
            return $"{GetGeoHash(centerPosition, 6)}-find_and_explore/points-of-interest";
        }

        string GetAreaCacheKey(Position centerPosition)
        {
            return $"{GetGeoHash(centerPosition, 6)}-find_and_explore/current-area";
        }

        private void LoadVenues_OnNext(ICollection<PointOfInterest> pointsOfInterest)
        {
            try
            {
                var pointsOfInterestFeatureCollection = pointsOfInterest.ToFeatureCollection();
                
                _pointOfInterestSource = new GeoJsonSource(GEOJSON_POI_SOURCE_ID, pointsOfInterestFeatureCollection);
                
                _schedulerProvider.MainThread.Schedule(_ =>
                {
                    _mapLayerController.AddSource(_pointOfInterestSource);
                    
                    SetUpPOIImage();
                    SetUpPOIMarkerLayer();
                    
                    Features = pointsOfInterestFeatureCollection;
                    var places = pointsOfInterest.ToPlaceCollection();
                    
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
                PointsOfInterest_OnError(exception);
            }
        }
        
        private void SetUpPOIImage()
        {
            _mapLayerController.AddImage(RED_MARKER_IMAGE_ID, "red_marker");
        }

        private void SetUpPOIMarkerLayer()
        {            
            _mapLayerController.AddLayer(new SymbolLayer(POI_MARKER_LAYER_ID, GEOJSON_POI_SOURCE_ID)
            {
                IconImage = Expression.Literal(RED_MARKER_IMAGE_ID),
                IconAllowOverlap = Expression.Literal(true),
                IconIgnorePlacement = Expression.Literal(true),
                IconOffset = Expression.Literal(new float[] { 0f, -8f })
            });
        }
        
        private void RefreshPointsOfInterest_OnNext(ICollection<PointOfInterest> pointsOfInterest)
        {
            try
            {
                UpdatePointsOfInterest(pointsOfInterest);
            }
            catch (Exception exception)
            {
                PointsOfInterest_OnError(exception);
            }
        }

        void UpdatePointsOfInterest(ICollection<PointOfInterest> pointsOfInterest)
        {
            var pointsOfInterestFeatureCollection = pointsOfInterest.ToFeatureCollection();
            
            _schedulerProvider.MainThread.Schedule(_ =>
            {
                _mapLayerController.UpdateSource(GEOJSON_POI_SOURCE_ID, pointsOfInterestFeatureCollection);
                
                Features = pointsOfInterestFeatureCollection;
                var places = pointsOfInterest.ToPlaceCollection();
                
                //using Edit locks the Cache so the operations within it are threadsafe
                ViewModelCache.Edit(innerCache =>
                {
                    ViewModelCache.Clear();
                    ViewModelCache.AddOrUpdate(places);
                });
            });
        }

        private void PointsOfInterest_OnError(Exception exception)
        {    
            _errorReporter.TrackError(exception);
        }
        
    }

}