
using System;
using System.Reactive;
using System.Reactive.Linq;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using Com.Mapbox.Geojson;
using Com.Mapbox.Mapboxsdk.Camera;
using Com.Mapbox.Mapboxsdk.Geometry;
using Com.Mapbox.Mapboxsdk.Location;
using Com.Mapbox.Mapboxsdk.Maps;
using Com.Mapbox.Mapboxsdk.Plugins.Annotation;
using Com.Mapbox.Mapboxsdk.Style.Layers;
using Com.Mapbox.Mapboxsdk.Style.Sources;
using CommonServiceLocator;
using DynamicData;
using DynamicData.Binding;
using FindAndExplore.Droid.Mapping;
using FindAndExplore.Droid.Presentation;
using FindAndExplore.Extensions;
using FindAndExplore.Mapping;
using FindAndExplore.ViewModels;
using GeoJSON.Net.Geometry;
using Java.Lang;
using ReactiveUI;

namespace FindAndExplore.Droid
{
    public class MapFragment : BaseFragment<MapViewModel>,
                                IOnMapReadyCallback,
                                Style.IOnStyleLoaded,
                                IOnSymbolClickListener,
                                MapboxMap.IOnMapClickListener,
                                MapboxMap.IOnCameraMoveListener,
                                MapboxMap.IOnFlingListener
    {
        static string GEOJSON_POI_SOURCE_ID = "GEOJSON_POI_SOURCE_ID";
        static string RED_MARKER_IMAGE_ID = "RED_MARKER_IMAGE_ID";
        static string POI_MARKER_LAYER_ID = "POI_MARKER_LAYER_ID";

        static string GEOJSON_VENUE_SOURCE_ID = "GEOJSON_VENUE_SOURCE_ID";
        static string BAR_MARKER_IMAGE_ID = "BAR_MARKER_IMAGE_ID";
        static string VENUE_MARKER_LAYER_ID = "VENUE_MARKER_LAYER_ID";

        readonly IMapControl _mapControl;
        readonly IMapLayerController _mapLayerController;
        
        MapView _mapView;
        MapboxMap _mapboxMap;
        SymbolManager _symbolManager;

        GeoJsonSource _pointsOfInterestSource;
        FeatureCollection _pointsOfInterestFeatureCollection;

        GeoJsonSource _venuesSource;
        FeatureCollection _venuesFeatureCollection;

        Style _style;
        
        public MapFragment()
        {
            _mapControl = ServiceLocator.Current.GetInstance<IMapControl>();
            _mapLayerController = ServiceLocator.Current.GetInstance<IMapLayerController>();
            ViewModel = ServiceLocator.Current.GetInstance<MapViewModel>();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.map_fragment_view, container, false);

            _mapView = view.FindViewById<MapView>(Resource.Id.mapView);
            _mapView.OnCreate(savedInstanceState);
            _mapView.GetMapAsync(this);

            // no longer used but keep as a reference for now
            /*
            var connection = ViewModel.PointsOfInterest.ToObservableChangeSet();
            connection.Subscribe(OnChanged);
            */
            
            return view;
        }
        
        public void OnMapReady(MapboxMap mapboxMap)
        {
            _mapboxMap = mapboxMap;
            
            _mapboxMap.SetStyle(Style.SATELLITE_STREETS, this);                      
            
            _mapboxMap.AddOnCameraMoveListener(this);
            _mapboxMap.AddOnFlingListener(this);
            _mapboxMap.AddOnMapClickListener(this);
            
            var move = Observable.FromEventPattern<MapView.StyleImageMissingEventArgs>(_mapView, "StyleImageMissing");
            move.Subscribe(evt =>
            {
                _mapControl.StyleImageMissing?.Execute();
            });
        }

        private void OnPointsOfInterestChanged(GeoJSON.Net.Feature.FeatureCollection featureCollection)
        {
            UpdatePointsOfInterestGeoSource();
        }

        private void OnVenuesChanged(GeoJSON.Net.Feature.FeatureCollection featureCollection)
        {
            UpdateVenuesGeoSource();
        }

        //refer to this example for Symbol Layer
        //https://docs.mapbox.com/android/maps/examples/symbol-layer-info-window/
        public void OnStyleLoaded(Style style)
        {
            _style = style;
            ((MapLayerController) _mapLayerController).MapStyle = _style;
            
            /*
            SetUpPOIImage();
            SetUpPOIMarkerLayer();
            */
            
            /*
            SetUpVenuesImage();
            SetUpVenuesMarkerLayer();
            */

            //this.WhenAnyValue(x => x.ViewModel.PointOfInterestFeatures).Subscribe(OnPointsOfInterestChanged);
            //this.WhenAnyValue(x => x.ViewModel.VenueFeatures).Subscribe(OnVenuesChanged);

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

            var mapStyle = new MapStyle
            {
                UrlString = style.Uri
            };
            _mapControl.DidFinishLoadingStyle?.Execute(mapStyle);
            
            // Have to set starting point on Android so setting way up high
            var position = new CameraPosition.Builder()
                .Target(new LatLng(51.137506, -3.008960))
                .Zoom(1)
                .Build();

            _mapboxMap.MoveCamera(CameraUpdateFactory.NewCameraPosition(position));
            
            var locationComponentOptions =
                LocationComponentOptions.InvokeBuilder(Activity);

            LocationComponentActivationOptions locationComponentActivationOptions = new LocationComponentActivationOptions
                .Builder(Activity, style)
                .LocationComponentOptions(locationComponentOptions.Build())
                .Build();

            var locationComponent = _mapboxMap.LocationComponent;
            locationComponent.ActivateLocationComponent(locationComponentActivationOptions);
            locationComponent.LocationComponentEnabled = true;
            _mapControl.LastKnownUserPosition = new Position(locationComponent.LastKnownLocation.Latitude, locationComponent.LastKnownLocation.Longitude);
            OnUserLocationFound(_mapControl.LastKnownUserPosition);

            _mapControl.DidFinishLoading?.Execute();
        }
        
        /*
        private void SetUpPOIImage()
        {
            var bitmap = BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.red_marker);
            _style.AddImage(RED_MARKER_IMAGE_ID, bitmap);
        }
        
        private void SetUpPOIMarkerLayer()
        {
            var symbolLayer = new SymbolLayer(POI_MARKER_LAYER_ID, GEOJSON_POI_SOURCE_ID)
                .WithProperties(new[]
                {
                    PropertyFactory.IconImage(RED_MARKER_IMAGE_ID),
                    PropertyFactory.IconAllowOverlap(Java.Lang.Boolean.True),
                    PropertyFactory.IconIgnorePlacement(Java.Lang.Boolean.True),
                    PropertyFactory.IconOffset(new Java.Lang.Float[] { new Java.Lang.Float(0.0f), new Java.Lang.Float(-9.0f) })
                });

            _style.AddLayer(symbolLayer);
        }
        
        private void SetUpVenuesImage()
        {
            var bitmap = BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.local_bar);
            _style.AddImage(BAR_MARKER_IMAGE_ID, bitmap);
        }

        private void SetUpVenuesMarkerLayer()
        {
            var symbolLayer = new SymbolLayer(VENUE_MARKER_LAYER_ID, GEOJSON_VENUE_SOURCE_ID)
                .WithProperties(new[]
                {
                    PropertyFactory.IconImage(BAR_MARKER_IMAGE_ID),
                    PropertyFactory.IconAllowOverlap(Java.Lang.Boolean.True),
                    PropertyFactory.IconIgnorePlacement(Java.Lang.Boolean.True),
                    PropertyFactory.IconOffset(new Java.Lang.Float[] { new Java.Lang.Float(0.0f), new Java.Lang.Float(-9.0f) })
                });

            _style.AddLayer(symbolLayer);
        }
        */
        
        public void OnCameraMove()
        {            
            _mapControl.Center = new Position(_mapboxMap.CameraPosition.Target.Latitude, _mapboxMap.CameraPosition.Target.Longitude);
        }

        public void OnFling()
        {            
            _mapControl.Center = new Position(_mapboxMap.CameraPosition.Target.Latitude, _mapboxMap.CameraPosition.Target.Longitude);
        }

        private void SetupPointsOfInterestGeoSource()
        {
            _pointsOfInterestSource = new GeoJsonSource(GEOJSON_POI_SOURCE_ID, _pointsOfInterestFeatureCollection);
            _style.AddSource(_pointsOfInterestSource);
        }
        
        private void UpdatePointsOfInterestGeoSource()
        {
            if(_pointsOfInterestSource == null)
            {
                SetupPointsOfInterestGeoSource();
            }
            else
            {
                _pointsOfInterestSource.SetGeoJson(FeatureCollection.FromJson(ViewModel.PointOfInterestFeatures.ToGeoJsonFeatureSource()));    
            }
        }

        private void SetupVenuesGeoSource()
        {
            _venuesSource = new GeoJsonSource(GEOJSON_VENUE_SOURCE_ID, _venuesFeatureCollection);
            _style.AddSource(_venuesSource);
        }

        private void UpdateVenuesGeoSource()
        {
            if (_venuesSource == null)
            {
                SetupVenuesGeoSource();
            }
            else
            {
                _venuesSource.SetGeoJson(FeatureCollection.FromJson(ViewModel.VenueFeatures.ToGeoJsonFeatureSource()));
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
                    // TODO Need to work out a way to remove each item one by one, probably use TextField
                    if (_symbolManager.Annotations != null && _symbolManager.Annotations.Size() > 0)
                    {
                        _symbolManager.DeleteAll();
                    }
                }
            }
        }*/

        private void OnUserLocationFound(Position position)
        {
            if (position != null)
            {
                var cameraPosition = new CameraPosition.Builder()
                        .Target(new LatLng(position.Latitude, position.Longitude))
                        .Zoom(13)
                        .Build();

                _mapboxMap.AnimateCamera(CameraUpdateFactory.NewCameraPosition(cameraPosition), 2000);
            }
        }

        public void OnAnnotationClick(Symbol symbol)
        {

        }

        public bool OnMapClick(LatLng clickPosition)
        {
            _mapControl.DidTapOnMap?.Execute(new Position(clickPosition.Latitude, clickPosition.Longitude));

            return true;
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
