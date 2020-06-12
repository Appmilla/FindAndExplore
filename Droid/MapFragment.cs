
using System;
using System.Reactive.Linq;

using Android.OS;
using Android.Views;
using Com.Mapbox.Mapboxsdk.Geometry;
using Com.Mapbox.Mapboxsdk.Maps;
using Com.Mapbox.Mapboxsdk.Plugins.Annotation;

using CommonServiceLocator;
using DynamicData;
using DynamicData.Binding;
using FindAndExplore.ViewModels;
using FindAndExploreApi.Client;
using GeoJSON.Net.Geometry;
using Java.Lang;
using ReactiveUI.AndroidSupport;

namespace FindAndExplore.Droid
{    
    public class MapFragment : ReactiveFragment<MapViewModel>,
                                IOnMapReadyCallback,
                                Style.IOnStyleLoaded,
                                IOnSymbolClickListener,
                                MapboxMap.IOnCameraMoveListener,
                                MapboxMap.IOnFlingListener
    {
        MapView _mapView;
        MapboxMap _mapboxMap;
        SymbolManager _symbolManager;

        public MapFragment()
        {
            ViewModel = ServiceLocator.Current.GetInstance<MapViewModel>();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.MapFragmentView, container, false);

            _mapView = view.FindViewById<MapView>(Resource.Id.mapView);
            _mapView.OnCreate(savedInstanceState);
            _mapView.GetMapAsync(this);

            var connection = ViewModel.PointsOfInterest.ToObservableChangeSet();
            connection.Subscribe(OnChanged);

            return view;
        }

        public void OnMapReady(MapboxMap mapboxMap)
        {
            _mapboxMap = mapboxMap;
            
            _mapboxMap.SetStyle(Style.SATELLITE_STREETS, this);                      
            
            _mapboxMap.AddOnCameraMoveListener(this);
            _mapboxMap.AddOnFlingListener(this);

            OnCameraMove();
            ViewModel.OnMapLoaded();
        }

        public void OnStyleLoaded(Style style)
        {
            _symbolManager = new SymbolManager(_mapView, _mapboxMap, style);

            // set non data driven properties
            _symbolManager.IconAllowOverlap = Java.Lang.Boolean.True;
            _symbolManager.TextAllowOverlap = Java.Lang.Boolean.True;

            _symbolManager.AddClickListener(this);
        }

        public void OnCameraMove()
        {
            ViewModel.CenterLocation = new Position(_mapboxMap.CameraPosition.Target.Latitude, _mapboxMap.CameraPosition.Target.Longitude);
        }

        public void OnFling()
        {
            ViewModel.CenterLocation = new Position(_mapboxMap.CameraPosition.Target.Latitude, _mapboxMap.CameraPosition.Target.Longitude);
        }

        private void OnChanged(IChangeSet<PointOfInterest> changeset)
        {
            foreach (var change in changeset)
            {
                if (change.Reason == ListChangeReason.AddRange)
                {                    
                    foreach (var addedPoint in change.Range)
                    {                        
                        var pointMarker = new SymbolOptions()
                            .WithIconImage("marker-15") //see https://github.com/mapbox/mapbox-gl-styles/blob/master/README.md
                            .WithIconSize(new Float(4.0f))                            
                            .WithLatLng(new LatLng(addedPoint.Location.Coordinates.Latitude, addedPoint.Location.Coordinates.Longitude));
                                                    
                        _symbolManager.Create(pointMarker);
                        
                    }
                }
                else if (change.Reason == ListChangeReason.Add)
                {
                    var pointMarker = new SymbolOptions()
                             .WithIconImage("marker-15") //see https://github.com/mapbox/mapbox-gl-styles/blob/master/README.md
                             .WithIconSize(new Float(4.0f))
                             .WithLatLng(new LatLng(change.Item.Current.Location.Coordinates.Latitude, change.Item.Current.Location.Coordinates.Longitude));

                    _symbolManager.Create(pointMarker);
                }
                else if (change.Reason == ListChangeReason.Remove)
                {
                    _symbolManager.DeleteAll();
                }
            }
        }

        public void OnAnnotationClick(Symbol symbol)
        {

        }

        public override void OnStart()
        {
            base.OnStart();
            _mapView.OnStart();
        }
        public override void OnResume()
        {
            base.OnResume();
            _mapView.OnResume();
        }
        public override void OnPause()
        {
            _mapView.OnPause();
            base.OnPause();
        }
        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            _mapView.OnSaveInstanceState(outState);
        }
        public override void OnStop()
        {
            base.OnStop();
            _mapView.OnStop();
        }
        public override void OnDestroyView()
        {
            _mapView.OnDestroy();
            base.OnDestroy();
        }
        public override void OnLowMemory()
        {
            base.OnLowMemory();
            _mapView.OnLowMemory();
        }        
    }
}
