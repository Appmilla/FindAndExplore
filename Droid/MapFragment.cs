
using System;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Com.Mapbox.Geojson;
using Com.Mapbox.Mapboxsdk.Camera;
using Com.Mapbox.Mapboxsdk.Geometry;
using Com.Mapbox.Mapboxsdk.Maps;
using Com.Mapbox.Mapboxsdk.Plugins.Annotation;
using Com.Mapbox.Mapboxsdk.Style.Layers;
using Com.Mapbox.Mapboxsdk.Style.Sources;
using CommonServiceLocator;
using FindAndExplore.Extensions;
using FindAndExplore.ViewModels;
using GeoJSON.Net.Geometry;
using ReactiveUI;

namespace FindAndExplore.Droid
{    
    public class MapFragment : ReactiveUI.AndroidSupport.ReactiveFragment<MapViewModel>,
                                IOnMapReadyCallback,
                                Style.IOnStyleLoaded,
                                IOnSymbolClickListener,
                                MapboxMap.IOnCameraMoveListener,
                                MapboxMap.IOnFlingListener
    {
        static string GEOJSON_SOURCE_ID = "GEOJSON_SOURCE_ID";
        static string MARKER_IMAGE_ID = "MARKER_IMAGE_ID";
        static string MARKER_LAYER_ID = "MARKER_LAYER_ID";
            
        MapView _mapView;
        MapboxMap _mapboxMap;
        SymbolManager _symbolManager;

        GeoJsonSource _source;
        FeatureCollection _featureCollection;

        Style _style;
        
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

            // no longer used but keep as a reference for now
            //var connection = ViewModel.PointsOfInterest.ToObservableChangeSet();
            //connection.Subscribe(OnChanged);

            return view;
        }
        
        public void OnMapReady(MapboxMap mapboxMap)
        {
            _mapboxMap = mapboxMap;
            
            _mapboxMap.SetStyle(Style.SATELLITE_STREETS, this);                      
            
            _mapboxMap.AddOnCameraMoveListener(this);
            _mapboxMap.AddOnFlingListener(this);

            ViewModel.OnMapLoaded();
        }

        private void OnNext(GeoJSON.Net.Feature.FeatureCollection featureCollection)
        {
            UpdateGeoSource();
        }

        //refer to this example for Symbol Layer
        //https://docs.mapbox.com/android/maps/examples/symbol-layer-info-window/
        public void OnStyleLoaded(Style style)
        {
            _style = style;
            
            SetUpImage();
            SetUpMarkerLayer();
            
            this.WhenAnyValue(x => x.ViewModel.Features).Subscribe(OnNext);

            // Leave for now as we may want to add markers using Symbol Manager and this is a useful reference
            /*
            _symbolManager = new SymbolManager(_mapView, _mapboxMap, style);
            
            // set non data driven properties
            _symbolManager.IconAllowOverlap = Java.Lang.Boolean.True;
            _symbolManager.IconIgnorePlacement = Java.Lang.Boolean.True;
            _symbolManager.TextAllowOverlap = Java.Lang.Boolean.True;
            _symbolManager.TextIgnorePlacement = Java.Lang.Boolean.True;

            _symbolManager.AddClickListener(this);
            */
            
            var position = new CameraPosition.Builder()
                .Target(new LatLng(51.137506, -3.008960))
                .Build();

            _mapboxMap.AnimateCamera(CameraUpdateFactory.NewCameraPosition(position), 2000);
        }
        
        private void SetUpImage()
        {
            var bitmap = BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.red_marker);
            _style.AddImage(MARKER_IMAGE_ID, bitmap);
        }
        
        private void SetUpMarkerLayer()
        {
            var symbolLayer = new SymbolLayer(MARKER_LAYER_ID, GEOJSON_SOURCE_ID)
                .WithProperties(new[]
                {
                    PropertyFactory.IconImage(MARKER_IMAGE_ID),
                    PropertyFactory.IconAllowOverlap(Java.Lang.Boolean.True),
                    PropertyFactory.IconOffset(new Java.Lang.Float[] { new Java.Lang.Float(0.0f), new Java.Lang.Float(-9.0f) })
                });

            _style.AddLayer(symbolLayer);
        }
        
        public void OnCameraMove()
        {
            ViewModel.CenterLocation = new Position(_mapboxMap.CameraPosition.Target.Latitude, _mapboxMap.CameraPosition.Target.Longitude);
        }

        public void OnFling()
        {
            ViewModel.CenterLocation = new Position(_mapboxMap.CameraPosition.Target.Latitude, _mapboxMap.CameraPosition.Target.Longitude);
        }

        private void SetupGeoSource()
        {
            _source = new GeoJsonSource(GEOJSON_SOURCE_ID, _featureCollection);
            _style.AddSource(_source);
        }
        
        private void UpdateGeoSource()
        {
            if(_source == null)
            {
                SetupGeoSource();
            }
            else
            {
                _source.SetGeoJson(FeatureCollection.FromJson(ViewModel.Features.ToGeoJsonFeatureSource()));    
            }
        }
        
        // Leave for now as we may want to add markers using Symbol Manager and this is a useful reference
        /*
        private void OnChanged(IChangeSet<PointOfInterest> changeset)
        {
            foreach (var change in changeset)
            {
                if (change.Reason == ListChangeReason.AddRange)
                {                    
                    foreach (var addedPoint in change.Range)
                    {                        
                        var pointMarker = new SymbolOptions()
                            .WithIconImage("tw-provincial-expy-2") //see https://github.com/mapbox/mapbox-gl-styles/blob/master/README.md
                            .WithIconSize(new Float(1.0f))                            
                            .WithLatLng(new LatLng(addedPoint.Location.Coordinates.Latitude, addedPoint.Location.Coordinates.Longitude));

                        _symbolManager.Create(pointMarker);
                        
                    }
                }
                else if (change.Reason == ListChangeReason.Add)
                {
                    var pointMarker = new SymbolOptions()
                             .WithIconImage("tw-provincial-expy-2") //see https://github.com/mapbox/mapbox-gl-styles/blob/master/README.md
                             .WithIconSize(new Float(1.0f))
                             .WithLatLng(new LatLng(change.Item.Current.Location.Coordinates.Latitude, change.Item.Current.Location.Coordinates.Longitude));

                    _symbolManager.Create(pointMarker);
                }
                else if (change.Reason == ListChangeReason.Remove)
                {
                    // Need to work out a way to remove each item one by one, probably use TextField
                    if (_symbolManager.Annotations != null && _symbolManager.Annotations.Size() > 0)
                    {
                        _symbolManager.DeleteAll();
                    }
                }
            }
        }
        */
        
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
