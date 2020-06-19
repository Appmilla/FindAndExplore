using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using FindAndExplore.DynamicData;
using FindAndExplore.Extensions;
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
using AsyncLock = FindAndExplore.Threading.AsyncLock;

namespace FindAndExplore.ViewModels
{
    public class MapViewModel : BaseViewModel
    {
        private const int Venues_Radius = 5000;

        private const string AnimationKeySpinningCircle = "PulsingCircle";
        private const string AnimationKeySuccess = "Success";

        private const int SpinningCircleAnimationStartFrame = 0;
        private const int SpinningCircleAnimationEndFrame = 40;
        private const int SuccessAnimationStartFrame = 60;
        private const int SuccessAnimationEndFrame = 95;

        public IList<AnimationSection> AnimationSequence { get; } = new List<AnimationSection>
        {
            new AnimationSection(AnimationKeySpinningCircle, SpinningCircleAnimationStartFrame,
                    SpinningCircleAnimationEndFrame),
            new AnimationSection(AnimationKeySuccess, SuccessAnimationStartFrame, SuccessAnimationEndFrame)
        };

        public string AnimationJson => "LocationOrangeCircle.json";

        readonly IFindAndExploreQuery _findAndExploreQuery;
        readonly IFoursquareQuery _foursquareQuery;
        readonly ISchedulerProvider _schedulerProvider;

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

        private static readonly Func<Venue, string> VenueKeySelector = venue => venue.Id;
        readonly SourceCache<Venue, string> _venuesCache = new SourceCache<Venue, string>(VenueKeySelector);


        /*
        private static readonly Func<FeatureCollection, string> FeatureCollectionKeySelector = fc => fc.;
        readonly SourceCache<FeatureCollection, string> _featureCollectionCache = new SourceCache<FeatureCollection, string>(FeatureCollectionKeySelector);
        */

        [Reactive]
        public FeatureCollection PointOfInterestFeatures { get; set; }

        [Reactive]
        public FeatureCollection VenueFeatures { get; set; }

        public ReactiveCommand<Unit, ICollection<PointOfInterest>> LoadPointsOfInterest { get; }

        public ReactiveCommand<Unit, ICollection<PointOfInterest>> RefreshPointsOfInterest { get; }

        public ReactiveCommand<Unit, ICollection<Venue>> LoadVenues { get; }

        public ReactiveCommand<Unit, ICollection<Venue>> RefreshVenues { get; }


        public ReactiveCommand<Unit, Unit> CancelInFlightQueries { get; }

        Geohasher _geohasher = new Geohasher();

        SupportedArea CurrentArea { get; set; }

        private Position _centerLocation = new Position(51.0664995383346f, -3.0453250843303f);

        public Position CenterLocation
        {
            get => _centerLocation;
            set
            {
                if (_centerLocation != value)
                {
                    this.RaiseAndSetIfChanged(ref _centerLocation, value);

                    if (_centerLocation.Latitude != 0 && _centerLocation.Longitude != 0)                        
                        CheckAndUpdateCurrentArea();
                }
            }
        }

        [Reactive]
        public Position UserLocation { get; set; }

        public MapViewModel(
            IFindAndExploreQuery findAndExploreQuery,
            IFoursquareQuery foursquareQuery,
            ISchedulerProvider schedulerProvider)
        {
            _findAndExploreQuery = findAndExploreQuery;
            _foursquareQuery = foursquareQuery;
            _schedulerProvider = schedulerProvider;
            
            this.WhenAnyValue(x => x._findAndExploreQuery.IsBusy)
                .ObserveOn(schedulerProvider.MainThread)
                .ToPropertyEx(this, x => x.IsBusy, scheduler: _schedulerProvider.MainThread);
            

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

            LoadVenues = ReactiveCommand.CreateFromObservable(
                () => _foursquareQuery.GetVenues(CenterLocation.Latitude, CenterLocation.Longitude, Venues_Radius, GetCacheKey()).TakeUntil(CancelInFlightQueries),
                this.WhenAnyValue(x => x.IsBusy).Select(x => !x),
                outputScheduler: _schedulerProvider.ThreadPool);
            LoadVenues.ThrownExceptions.Subscribe(Venues_OnError);
            LoadVenues.Subscribe(Venues_OnNext);

            RefreshVenues = ReactiveCommand.CreateFromObservable(
                () => _foursquareQuery.RefreshVenues(CenterLocation.Latitude, CenterLocation.Longitude, Venues_Radius, GetCacheKey()).TakeUntil(CancelInFlightQueries),
                this.WhenAnyValue(x => x.IsBusy).Select(x => !x),
                outputScheduler: _schedulerProvider.ThreadPool);
            RefreshVenues.ThrownExceptions.Subscribe(Venues_OnError);
            RefreshVenues.Subscribe(Venues_OnNext);

            CancelInFlightQueries = ReactiveCommand.Create(
                () => { },
                this.WhenAnyObservable(
                    a => a.LoadPointsOfInterest.IsExecuting,
                    b => b.RefreshPointsOfInterest.IsExecuting,
                    c => c.LoadVenues.IsExecuting,
                    d => d.RefreshVenues.IsExecuting,
                    (a, b, c, d) => a || b || c || d));

            _ = _pointsOfInterestCache.Connect()
                .Bind(out _pointsOfInterest)
                .ObserveOn(schedulerProvider.MainThread)        //ensure operation is on the UI thread;
                .DisposeMany()                              //automatic disposal
                .Subscribe();

            _ = _venuesCache.Connect()
                .Bind(out _venues)
                .ObserveOn(schedulerProvider.MainThread)        //ensure operation is on the UI thread;
                .DisposeMany()                              //automatic disposal
                .Subscribe();
        }        

        public async Task OnMapLoaded()
        {
            PopupPresenter.ProgressAnimationCompleted += ProgressAnimationCompletedAsync;

            PopupPresenter.ShowProgress("Let's see if we can find where you are...",
                AnimationJson, AnimationSequence);

            // Here we can go and get your current location this will come out
            await Task.Delay(2500);

            // Success animation
            PopupPresenter?.UpdateProgress("Ah ha, I found you!", AnimationKeySuccess);
        }

        string GetGeoHash()
        {
            //https://github.com/postlagerkarte/geohash-dotnet
            //https://www.elastic.co/guide/en/elasticsearch/guide/current/geohashes.html#geohashes

            //precision level 5 is approx 4.9km x 4.9km so as long as the current map centre is within that square then the previously cached data can be used
            //precision level 6 is approx 1.2km x 0.61km 
            var geoHash = _geohasher.Encode(CenterLocation.Latitude, CenterLocation.Longitude, 6);
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
            _schedulerProvider.MainThread.Schedule(_ =>
            {
                PointOfInterestFeatures = pointsOfInterest.ToFeatureCollection();
                _pointsOfInterestCache.UpdateCache(pointsOfInterest, PointsOfInterestKeySelector);
            });
        }

        void PointsOfInterest_OnError(Exception e)
        {

        }

        private void Venues_OnNext(ICollection<Venue> venues)
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
            _schedulerProvider.MainThread.Schedule(_ =>
            {
                VenueFeatures = venues.ToFeatureCollection();
                _venuesCache.UpdateCache(venues, VenueKeySelector);
            });
        }

        private void Venues_OnError(Exception obj)
        {            
        }

        readonly AsyncLock Mutex = new AsyncLock();

        void CheckAndUpdateCurrentArea()
        {
            Task.Run(async () =>
            {
                var mutexLock = await Mutex.LockAsync();

                try
                {
                    Observable.Return(Unit.Default).InvokeCommand(RefreshVenues);

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
                finally
                {
                    mutexLock.Dispose();
                }
            });
        }


        async Task<SupportedArea> GetCurrentArea()
        {
            var areas = await _findAndExploreQuery.GetCurrentArea(CenterLocation.Latitude, CenterLocation.Longitude, GetAreaCacheKey());

            return areas?.SingleOrDefault();
        }

        private async void ProgressAnimationCompletedAsync(object sender, AnimationSection animationSection)
        {
            if (animationSection.Key.Equals(AnimationKeySuccess))
            {
                // Delay so that the finish animation is shown
                await Task.Delay(1000);

                // Set the found user location
                UserLocation = new Position(51.137506, -3.008960);
                PopupPresenter.DismissProgress();
            }
        }
    }
}