using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DynamicData;
using FacebookApi.Client;
using FindAndExplore.Extensions;
using FindAndExplore.Infrastructure;
using FindAndExplore.Mapping;
using FindAndExplore.Mapping.Expressions;
using FindAndExplore.Mapping.Layers;
using FindAndExplore.Mapping.Sources;
using FindAndExplore.Queries;
using FindAndExplore.Reactive;
using FindAndExplore.ViewModels;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.DatasetProviders
{
    public interface IFacebookDatasetProvider
    {
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        bool IsBusy { get; }

        ReactiveCommand<Position, ICollection<Place>> Load { get; }
        ReactiveCommand<Position, ICollection<Place>> Refresh { get; }
        ReactiveCommand<Unit, Unit> CancelInFlightQueries { get; }
        SourceCache<PlaceViewModel, String> ViewModelCache { get; }
        FeatureCollection Features { get; }
    }

    public class FacebookDatasetProvider : DatasetProvider<PlaceViewModel, Place, ICollection<Place>, string>, IFacebookDatasetProvider
    {
        private const int Venues_Radius = 5000;

        private const string GEOJSON_FACEBOOK_SOURCE_ID = "GEOJSON_FACEBOOK_SOURCE_ID";
        private const string BLUE_MARKER_IMAGE_ID = "BLUE_MARKER_IMAGE_ID";
        private const string FACEBOOK_MARKER_LAYER_ID = "FACEBOOK_MARKER_LAYER_ID";

        readonly IFacebookQuery _facebookQuery;
        readonly IMapLayerController _mapLayerController;
        readonly ISchedulerProvider _schedulerProvider;
        readonly IErrorReporter _errorReporter;

        private GeoJsonSource _placesSource;

        private static readonly Func<PlaceViewModel, string> PlacesKeySelector = place => place.Id;

        [Reactive]
        public FeatureCollection Features { get; set; }

        public FacebookDatasetProvider(
            IFacebookQuery facebookQuery,
            IMapLayerController mapLayerController,
            ISchedulerProvider schedulerProvider,
            IErrorReporter errorReporter)
        {
            _facebookQuery = facebookQuery;
            _mapLayerController = mapLayerController;
            _schedulerProvider = schedulerProvider;
            _errorReporter = errorReporter;

            ViewModelCache = new SourceCache<PlaceViewModel, string>(PlacesKeySelector);

            Load = ReactiveCommand.CreateFromObservable<Position, ICollection<Place>>(
                OnLoad,
                this.WhenAnyValue(x => x.IsBusy).Select(x => !x),
                outputScheduler: _schedulerProvider.ThreadPool);
            Load.ThrownExceptions.Subscribe(Places_OnError);
            Load.Subscribe(LoadPlaces_OnNext);

            Refresh = ReactiveCommand.CreateFromObservable<Position, ICollection<Place>>(
                OnRefresh,
                this.WhenAnyValue(x => x.IsBusy).Select(x => !x),
                outputScheduler: _schedulerProvider.ThreadPool);
            Refresh.ThrownExceptions.Subscribe(Places_OnError);
            Refresh.Subscribe(RefreshPlaces_OnNext);

            CancelInFlightQueries = ReactiveCommand.Create(
                () => { },
                this.WhenAnyObservable(
                    l => l.Load.IsExecuting,
                    r => r.Refresh.IsExecuting,
                    (l, r) => l || r));
        }

        string GetCacheKey(Position centerPosition)
        {
            return $"{GetGeoHash(centerPosition, 6)}-find_and_explore/facebook-places";
        }

        private IObservable<ICollection<Place>> OnLoad(Position centerPosition)
        {
            return _facebookQuery
                .GetPlaces(centerPosition.Latitude, centerPosition.Longitude, Venues_Radius, GetCacheKey(centerPosition))
                .TakeUntil(CancelInFlightQueries);
        }

        private IObservable<ICollection<Place>> OnRefresh(Position centerPosition)
        {
            return _facebookQuery
                .RefreshPlaces(centerPosition.Latitude, centerPosition.Longitude, Venues_Radius, GetCacheKey(centerPosition))
                .TakeUntil(CancelInFlightQueries);
        }

        private void LoadPlaces_OnNext(ICollection<Place> places)
        {
            try
            {
                var placesFeatureCollection = places.ToFeatureCollection();

                _placesSource = new GeoJsonSource(GEOJSON_FACEBOOK_SOURCE_ID, placesFeatureCollection);

                _schedulerProvider.MainThread.Schedule(_ =>
                {
                    _mapLayerController.AddSource(_placesSource);

                    SetUpVenuesImage();
                    SetUpVenuesMarkerLayer();

                    Features = placesFeatureCollection;
                    var placesCollection = places.ToPlaceCollection();

                    //using Edit locks the Cache so the operations within it are threadsafe
                    ViewModelCache.Edit(innerCache =>
                    {
                        ViewModelCache.Clear();
                        ViewModelCache.AddOrUpdate(placesCollection);
                    });
                });
            }
            catch (Exception exception)
            {
                Places_OnError(exception);
            }
        }

        private void SetUpVenuesImage()
        {
            _mapLayerController.AddImage(BLUE_MARKER_IMAGE_ID, "blue_marker");
        }

        private void SetUpVenuesMarkerLayer()
        {
            _mapLayerController.AddLayer(new SymbolLayer(FACEBOOK_MARKER_LAYER_ID, GEOJSON_FACEBOOK_SOURCE_ID)
            {
                IconImage = Expression.Literal(BLUE_MARKER_IMAGE_ID),
                IconAllowOverlap = Expression.Literal(true),
                IconIgnorePlacement = Expression.Literal(true),
                IconOffset = Expression.Literal(new float[] { 0f, -8f })
            });
        }

        private void RefreshPlaces_OnNext(ICollection<Place> places)
        {
            try
            {
                UpdatePlaces(places);
            }
            catch (Exception exception)
            {
                Places_OnError(exception);
            }
        }

        void UpdatePlaces(ICollection<Place> places)
        {
            var placesFeatureCollection = places.ToFeatureCollection();

            _schedulerProvider.MainThread.Schedule(_ =>
            {
                _mapLayerController.UpdateSource(GEOJSON_FACEBOOK_SOURCE_ID, placesFeatureCollection);

                Features = placesFeatureCollection;
                var placesCollection = places.ToPlaceCollection();

                //using Edit locks the Cache so the operations within it are threadsafe
                ViewModelCache.Edit(innerCache =>
                {
                    ViewModelCache.Clear();
                    ViewModelCache.AddOrUpdate(placesCollection);
                });
            });
        }

        private void Places_OnError(Exception exception)
        {
            _errorReporter.TrackError(exception);
        }
    }
}
