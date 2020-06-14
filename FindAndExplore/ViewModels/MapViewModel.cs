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
using FindAndExplore.Queries;
using FindAndExplore.Reactive;
using FindAndExploreApi.Client;
using Geohash;
using GeoJSON.Net.Geometry;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using AsyncLock = FindAndExplore.Threading.AsyncLock;

namespace FindAndExplore.ViewModels
{
    public class MapViewModel : ReactiveObject
    {
        readonly IFindAndExploreQuery _findAndExploreQuery;
        readonly ISchedulerProvider _schedulerProvider;

        ReadOnlyObservableCollection<PointOfInterest> _pointsOfInterest;

        public ReadOnlyObservableCollection<PointOfInterest> PointsOfInterest
        {
            get => _pointsOfInterest;
            set => this.RaiseAndSetIfChanged(ref _pointsOfInterest, value);
        }

        [ObservableAsProperty]
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public bool IsBusy { get; }

        private static readonly Func<PointOfInterest, string> KeySelector = point => point.Id;
        readonly SourceCache<PointOfInterest, string> _pointsOfInterestCache = new SourceCache<PointOfInterest, string>(KeySelector);

        public ReactiveCommand<Unit, ICollection<PointOfInterest>> Load { get; }

        public ReactiveCommand<Unit, ICollection<PointOfInterest>> Refresh { get; }

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
                    CheckAndUpdateCurrentArea();
                }
            }
        }

        public MapViewModel(IFindAndExploreQuery findAndExploreQuery, ISchedulerProvider schedulerProvider)
        {
            _findAndExploreQuery = findAndExploreQuery;
            _schedulerProvider = schedulerProvider;

            /*
            this.WhenAnyValue(x => x._findAndExploreQuery.IsBusy)
                .ObserveOn(schedulerProvider.MainThread)
                .ToPropertyEx(this, x => x.IsBusy, scheduler: _schedulerProvider.MainThread);
            */

            Load = ReactiveCommand.CreateFromObservable(
                () => _findAndExploreQuery.GetPointsOfInterest(CurrentArea.LocationId, GetCacheKey()).TakeUntil(CancelInFlightQueries),
                //this.WhenAnyValue(x => x.IsBusy).Select(x => !x),
                outputScheduler: _schedulerProvider.ThreadPool);
            Load.ThrownExceptions.Subscribe(PointsOfInterest_OnError);
            Load.Subscribe(PointsOfInterest_OnNext);

            Refresh = ReactiveCommand.CreateFromObservable(
                () => _findAndExploreQuery.RefreshPointsOfInterest(CurrentArea.LocationId, GetCacheKey()).TakeUntil(CancelInFlightQueries),
                //this.WhenAnyValue(x => x.IsBusy).Select(x => !x),
                outputScheduler: _schedulerProvider.ThreadPool);
            Refresh.ThrownExceptions.Subscribe(PointsOfInterest_OnError);
            Refresh.Subscribe(PointsOfInterest_OnNext);

            CancelInFlightQueries = ReactiveCommand.Create(
                () => { },
                this.WhenAnyObservable(x => x.Load.IsExecuting, y => y.Refresh.IsExecuting, (x, y) => x || y));

            _ = _pointsOfInterestCache.Connect()
                .Bind(out _pointsOfInterest)
                .ObserveOn(schedulerProvider.MainThread)        //ensure operation is on the UI thread;
                .DisposeMany()                              //automatic disposal
                .Subscribe();
        }

        public void OnMapLoaded()
        {

        }

        string GetGeoHash()
        {
            //https://github.com/postlagerkarte/geohash-dotnet
            //https://www.elastic.co/guide/en/elasticsearch/guide/current/geohashes.html#geohashes

            //precision level 5 is approx 4.9km x 4.9km so as long as the current map centre is within that square then the previously cached data can be used
            var geoHash = _geohasher.Encode(CenterLocation.Latitude, CenterLocation.Longitude, 5);
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
                _pointsOfInterestCache.UpdateCache(pointsOfInterest, KeySelector);
            });
        }

        void PointsOfInterest_OnError(Exception e)
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
                    var area = await GetCurrentArea();
                    if (area != null)
                    {
                        if (area?.LocationId != CurrentArea?.LocationId)
                        {
                            CurrentArea = area;

                            Observable.Return(Unit.Default).InvokeCommand(Refresh);
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
    }
}

