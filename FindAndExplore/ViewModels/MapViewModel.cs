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
        readonly ISchedulerProvider _schedulerProvider;
        readonly IErrorReporter _errorReporter;
        readonly IFoursquareDatasetProvider _foursquareDatasetProvider;
        readonly IFindAndExploreDatasetProvider _findAndExploreDatasetProvider;
        
        readonly Subject<Position> _sourceMapCenter = new Subject<Position>();
        
        bool _datasetProvidersLoaded;
       
        ReadOnlyObservableCollection<PlaceViewModel> _places;

        public ReadOnlyObservableCollection<PlaceViewModel> Places
        {
            get => _places;
            set => this.RaiseAndSetIfChanged(ref _places, value);
        }

        [ObservableAsProperty]
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public bool IsBusy { get; }

        [ObservableAsProperty]
        public FeatureCollection PointOfInterestFeatures { get; set; }

        [ObservableAsProperty]
        public FeatureCollection VenueFeatures { get; }

        public ReactiveCommand<Position, Unit> MapCenterLocationChanged { get; }
        
        public ReactiveCommand<Unit, Unit> CancelInFlightQueries { get; }
        
        static readonly Func<PlaceViewModel, string> PlacesKeySelector = place => place.Id;
        
        public SourceCache<PlaceViewModel, string> ViewModelCache { get; protected set; }
        
        public MapViewModel(
            IMapControl mapControl,
            ISchedulerProvider schedulerProvider,
            IErrorReporter errorReporter,
            IFoursquareDatasetProvider foursquareDatasetProvider,
            IFindAndExploreDatasetProvider findAndExploreDatasetProvider)
        {
            _mapControl = mapControl;
            _schedulerProvider = schedulerProvider;
            _errorReporter = errorReporter;
            _foursquareDatasetProvider = foursquareDatasetProvider;
            _findAndExploreDatasetProvider = findAndExploreDatasetProvider;
            
            //The DatasetProviders populate a shared cache in this viewmodel to power the
            //various list views. Expect to move this cache to it's own object later
            //See how this works when we get a UI to display the items, seems simpler than 
            //combining multiple caches
            ViewModelCache = new SourceCache<PlaceViewModel, string>(PlacesKeySelector);
            _foursquareDatasetProvider.ViewModelCache = ViewModelCache;
            _findAndExploreDatasetProvider.ViewModelCache = ViewModelCache;
            
            this.WhenAnyValue(x => x._foursquareDatasetProvider.IsBusy, y => y._findAndExploreDatasetProvider.IsBusy, (x, y) => x || y)
                .ObserveOn(schedulerProvider.MainThread)
                .ToPropertyEx(this, x => x.IsBusy, scheduler: _schedulerProvider.MainThread);            
            
            this.WhenAnyValue(x => x._foursquareDatasetProvider.Features)
                .ObserveOn(schedulerProvider.MainThread)
                .ToPropertyEx(this, x => x.VenueFeatures, scheduler: _schedulerProvider.MainThread);
            
            this.WhenAnyValue(x => x._findAndExploreDatasetProvider.Features)
                .ObserveOn(schedulerProvider.MainThread)
                .ToPropertyEx(this, x => x.PointOfInterestFeatures, scheduler: _schedulerProvider.MainThread);
            
            MapCenterLocationChanged = ReactiveCommand.CreateFromTask<Position, Unit>
                ( _ => OnMapCenterLocationChanged(),
                outputScheduler: schedulerProvider.ThreadPool);
            MapCenterLocationChanged.ThrownExceptions.Subscribe(errorReporter.TrackError);
            
            _sourceMapCenter.InvokeCommand(_foursquareDatasetProvider.Refresh);
            _sourceMapCenter.InvokeCommand(_findAndExploreDatasetProvider.Refresh);

            CancelInFlightQueries = ReactiveCommand.Create(
                () => { },
                this.WhenAnyObservable(
                    a => a._findAndExploreDatasetProvider.Load.IsExecuting,
                    b => b._findAndExploreDatasetProvider.Refresh.IsExecuting,
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
            
            _ = ViewModelCache.Connect()
                .Bind(out _places)
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
                if (!_datasetProvidersLoaded)
                {
                    LoadDatasetProviders();
                }
                else
                {
                    _sourceMapCenter.OnNext(_mapControl.Center);
                    
                    //using Edit locks the Cache so the operations within it are threadsafe
                    ViewModelCache.Edit(innerCache =>
                    {
                        ViewModelCache.Clear();
                    });
                    
                }
            }
            catch (Exception exception)
            {
                _errorReporter.TrackError(exception);
            }            

            return Unit.Default;
        }

        private void LoadDatasetProviders()
        {
            _datasetProvidersLoaded = true;

            ViewModelCache.Clear();
            //TODO see if we can call this with the InvokeCommand syntax
            _foursquareDatasetProvider.Load.Execute(_mapControl.LastKnownUserPosition).Subscribe();
            _findAndExploreDatasetProvider.Load.Execute(_mapControl.LastKnownUserPosition).Subscribe();
        }

        private async Task<Unit> OnMapStyleLoaded(MapStyle mapStyle)
        {
            return Unit.Default;
        }
        
        private async Task<Unit> OnMapLoaded()
        {
            if (_mapControl.LastKnownUserPosition != null)
            {
                if (!_datasetProvidersLoaded)
                {
                    LoadDatasetProviders();
                }
            }

            return Unit.Default;
        }

        private async  Task<Unit> OnMapTapped(Position tapPosition)
        {
            return Unit.Default;
        }
    }
}