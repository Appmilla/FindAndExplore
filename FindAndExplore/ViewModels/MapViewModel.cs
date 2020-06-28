using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using FindAndExplore.DatasetProviders;
using FindAndExplore.DynamicData;
using FindAndExplore.Extensions;
using FindAndExplore.Infrastructure;
using FindAndExplore.Mapping;
using FindAndExplore.Presentation;
using FindAndExplore.Queries;
using FindAndExplore.Reactive;
using FindAndExploreApi.Client;
using FoursquareApi.Client;
using Geohash;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.ViewModels
{
    public class MapViewModel : BaseViewModel
    {
        //public const string AnimationKeySuccess = "Success";
        //public const string AnimationKeySpinningCircle = "PulsingCircle";

        //private const int SpinningCircleAnimationStartFrame = 0;
        //private const int SpinningCircleAnimationEndFrame = 40;
        //private const int SuccessAnimationStartFrame = 60;
        //private const int SuccessAnimationEndFrame = 95;

        //public IList<AnimationSection> AnimationSequence { get; } = new List<AnimationSection>
        //{
        //    new AnimationSection(AnimationKeySpinningCircle, SpinningCircleAnimationStartFrame,
        //            SpinningCircleAnimationEndFrame),
        //    new AnimationSection(AnimationKeySuccess, SuccessAnimationStartFrame, SuccessAnimationEndFrame)
        //};

        //public string AnimationJson => "LocationOrangeCircle.json";
        
        readonly IMapControl _mapControl;
        readonly IFindAndExploreQuery _findAndExploreQuery;
        readonly ISchedulerProvider _schedulerProvider;
        readonly IErrorReporter _errorReporter;
        readonly IFoursquareDatasetProvider _foursquareDatasetProvider;
        
        readonly Subject<Position> _sourceMapCenter = new Subject<Position>();
        
        ReadOnlyObservableCollection<PointOfInterest> _pointsOfInterest;

        public ReadOnlyObservableCollection<PointOfInterest> PointsOfInterest
        {
            get => _pointsOfInterest;
            set => this.RaiseAndSetIfChanged(ref _pointsOfInterest, value);
        }

        ReadOnlyObservableCollection<Venue> _venues;

        public ReadOnlyObservableCollection<Venue> Venues
        {
            get => _venues;
            set => this.RaiseAndSetIfChanged(ref _venues, value);
        }

        [ObservableAsProperty]
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public bool IsBusy { get; }

        private static readonly Func<PointOfInterest, string> PointsOfInterestKeySelector = point => point.Id;
        readonly SourceCache<PointOfInterest, string> _pointsOfInterestCache = new SourceCache<PointOfInterest, string>(PointsOfInterestKeySelector);

        [Reactive]
        public FeatureCollection PointOfInterestFeatures { get; set; }

        [ObservableAsProperty]
        public FeatureCollection VenueFeatures { get; }

        public ReactiveCommand<Position, Unit> MapCenterLocationChanged { get; }
        
        public ReactiveCommand<Unit, ICollection<PointOfInterest>> LoadPointsOfInterest { get; }

        public ReactiveCommand<Unit, ICollection<PointOfInterest>> RefreshPointsOfInterest { get; }
        
        public ReactiveCommand<Unit, Unit> CancelInFlightQueries { get; }

        Geohasher _geohasher = new Geohasher();

        SupportedArea CurrentArea { get; set; }
        
        public MapViewModel(
            IMapControl mapControl,
            IFindAndExploreQuery findAndExploreQuery,
            ISchedulerProvider schedulerProvider,
            IErrorReporter errorReporter,
            IFoursquareDatasetProvider foursquareDatasetProvider)
        {
            _mapControl = mapControl;
            _findAndExploreQuery = findAndExploreQuery;
            _schedulerProvider = schedulerProvider;
            _errorReporter = errorReporter;
            _foursquareDatasetProvider = foursquareDatasetProvider;
            
            this.WhenAnyValue(x => x._findAndExploreQuery.IsBusy)
                .ObserveOn(schedulerProvider.MainThread)
                .ToPropertyEx(this, x => x.IsBusy, scheduler: _schedulerProvider.MainThread);
            
            this.WhenAnyValue(x => x._foursquareDatasetProvider.Features)
                .ObserveOn(schedulerProvider.MainThread)
                .ToPropertyEx(this, x => x.VenueFeatures, scheduler: _schedulerProvider.MainThread);
            
            MapCenterLocationChanged = ReactiveCommand.CreateFromTask<Position, Unit>
                ( _ => OnMapCenterLocationChanged(),
                outputScheduler: schedulerProvider.ThreadPool);
            MapCenterLocationChanged.ThrownExceptions.Subscribe(errorReporter.TrackError);
            
            LoadPointsOfInterest = ReactiveCommand.CreateFromObservable(
                () => _findAndExploreQuery.GetPointsOfInterest(CurrentArea.LocationId, GetCacheKey()).TakeUntil(CancelInFlightQueries),
                this.WhenAnyValue(x => x.IsBusy).Select(x => !x),
                outputScheduler: _schedulerProvider.ThreadPool);
            LoadPointsOfInterest.ThrownExceptions.Subscribe(PointsOfInterest_OnError);
            LoadPointsOfInterest.Subscribe(PointsOfInterest_OnNext);

            RefreshPointsOfInterest = ReactiveCommand.CreateFromObservable(
                () => _findAndExploreQuery.RefreshPointsOfInterest(CurrentArea.LocationId, GetCacheKey()).TakeUntil(CancelInFlightQueries),
                this.WhenAnyValue(x => x.IsBusy).Select(x => !x),
                outputScheduler: _schedulerProvider.ThreadPool);
            RefreshPointsOfInterest.ThrownExceptions.Subscribe(PointsOfInterest_OnError);
            RefreshPointsOfInterest.Subscribe(PointsOfInterest_OnNext);

            _sourceMapCenter.InvokeCommand(_foursquareDatasetProvider.Refresh);

            CancelInFlightQueries = ReactiveCommand.Create(
                () => { },
                this.WhenAnyObservable(
                    a => a.LoadPointsOfInterest.IsExecuting,
                    b => b.RefreshPointsOfInterest.IsExecuting,
                    c => c._foursquareDatasetProvider.Load.IsExecuting,
                    d => d._foursquareDatasetProvider.Refresh.IsExecuting,
                    (a, b, c, d) => a || b || c || d));

            this.WhenAnyValue(x => x._mapControl.Center)
                .Throttle(TimeSpan.FromSeconds(0.2), schedulerProvider.ThreadPool)
                .DistinctUntilChanged()
                .ObserveOn(schedulerProvider.ThreadPool)
                .InvokeCommand(MapCenterLocationChanged);

            _mapControl.DidFinishLoadingStyle = ReactiveCommand.CreateFromTask<MapStyle, Unit>
            ( 
                OnMapStyleLoaded,
                outputScheduler: schedulerProvider.ThreadPool);
            _mapControl.DidFinishLoadingStyle.ThrownExceptions.Subscribe(errorReporter.TrackError);
            
            _mapControl.DidFinishLoading = ReactiveCommand.CreateFromTask<Unit, Unit>
            ( 
                _ => OnMapLoaded(),
                outputScheduler: schedulerProvider.ThreadPool);
            _mapControl.DidFinishLoading.ThrownExceptions.Subscribe(errorReporter.TrackError);
            
            _mapControl.DidTapOnMap = ReactiveCommand.CreateFromTask<Position, Unit>
            ( 
                OnMapTapped,
                outputScheduler: schedulerProvider.ThreadPool);
            _mapControl.DidTapOnMap.ThrownExceptions.Subscribe(errorReporter.TrackError);
            
            _ = _pointsOfInterestCache.Connect()
                .Bind(out _pointsOfInterest)
                .ObserveOn(schedulerProvider.MainThread)        //ensure operation is on the UI thread;
                .DisposeMany()                              //automatic disposal
                .Subscribe();

            _ = _foursquareDatasetProvider.ViewModelCache.Connect()
                .Bind(out _venues)
                .ObserveOn(schedulerProvider.MainThread)        //ensure operation is on the UI thread;
                .DisposeMany()                              //automatic disposal
                .Subscribe();
        }
        
        private async Task<Unit> OnMapCenterLocationChanged()
        {           
            if (_mapControl.Center == null)
                return Unit.Default;

            if (_mapControl.Center.Latitude == 0 && _mapControl.Center.Longitude == 0)
                return Unit.Default;

            try
            {
                //_foursquareDatasetProvider.Refresh.Execute(_mapControl.Center).Subscribe();
                _sourceMapCenter.OnNext(_mapControl.Center);
                
                var area = await GetCurrentArea();
                if (area != null)
                {
                    if (area?.LocationId != CurrentArea?.LocationId)
                    {
                        CurrentArea = area;

                        Observable.Return(Unit.Default).InvokeCommand(RefreshPointsOfInterest);
                    }
                }
            }
            catch (Exception exception)
            {
            }            

            return Unit.Default;
        }     
        
        private async Task<Unit> OnMapStyleLoaded(MapStyle mapStyle)
        {
            return Unit.Default;
        }
        
        private async Task<Unit> OnMapLoaded()
        {
            return Unit.Default;
        }

        private async  Task<Unit> OnMapTapped(Position tapPosition)
        {
            return Unit.Default;
        }
        
        string GetGeoHash()
        {
            //https://github.com/postlagerkarte/geohash-dotnet
            //https://www.elastic.co/guide/en/elasticsearch/guide/current/geohashes.html#geohashes

            //precision level 5 is approx 4.9km x 4.9km so as long as the current map centre is within that square then the previously cached data can be used
            //precision level 6 is approx 1.2km x 0.61km 
            var geoHash = _geohasher.Encode(_mapControl.Center.Latitude, _mapControl.Center.Longitude, 6);
            return geoHash;
        }

        string GetCacheKey()
        {
            return $"{GetGeoHash()}-find_and_explore/points-of-interest";
        }

        string GetAreaCacheKey()
        {
            return $"{GetGeoHash()}-find_and_explore/current-area";
        }

        void PointsOfInterest_OnNext(ICollection<PointOfInterest> pointsOfInterest)
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
                PointOfInterestFeatures = pointsOfInterestFeatureCollection;
                _pointsOfInterestCache.UpdateCache(pointsOfInterest, PointsOfInterestKeySelector);
            });
        }

        void PointsOfInterest_OnError(Exception e)
        {

        }

        async Task<SupportedArea> GetCurrentArea()
        {
            var areas = await _findAndExploreQuery.GetCurrentArea(_mapControl.Center.Latitude, _mapControl.Center.Longitude, GetAreaCacheKey());

            return areas?.SingleOrDefault();
        }
    }
}