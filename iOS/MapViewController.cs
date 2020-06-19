using System;
using CommonServiceLocator;
using CoreAnimation;
using CoreLocation;
using FindAndExplore.Extensions;
using FindAndExplore.iOS.Presentation;
using FindAndExplore.Mapping;
using FindAndExplore.ViewModels;
using Foundation;
using GeoJSON.Net.Geometry;
using Mapbox;
using ReactiveUI;
using UIKit;

namespace FindAndExplore.iOS
{
    public interface IMapController
    {
        void OnStyleLoaded(MGLStyle style);
        
        void OnMapLoaded();
    }
    
    public partial class MapViewController : BaseView<MapViewModel>, IMapController
    {
        static string GEOJSON_POI_SOURCE_ID = "GEOJSON_SOURCE_ID";
        static string RED_MARKER_IMAGE_ID = "MARKER_IMAGE_ID";
        static string POI_MARKER_LAYER_ID = "MARKER_LAYER_ID";

        static string GEOJSON_VENUE_SOURCE_ID = "GEOJSON_VENUE_SOURCE_ID";
        static string BAR_MARKER_IMAGE_ID = "BAR_MARKER_IMAGE_ID";
        static string VENUE_MARKER_LAYER_ID = "VENUE_MARKER_LAYER_ID";

        readonly IMapControl _mapControl;
        
        private MGLMapView _mapView;

        private MGLShapeSource _pointsOfInterestSource;
        private MGLShapeSource _venuesSource;

        private MGLStyle _style;
        
        public MapViewController (IntPtr handle) : base (handle)
        {
            _mapControl = ServiceLocator.Current.GetInstance<IMapControl>();
            ViewModel = ServiceLocator.Current.GetInstance<MapViewModel>();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = "Map";
            TabBarItem.Image = UIImage.FromBundle("tab_map_icon");

            _mapView = new MGLMapView(View.Bounds, MGLStyle.SatelliteStreetsStyleURL)
            {
                Delegate = new AppMGLMapViewDelegate
                {
                    MapControl = _mapControl,
                    ViewModel = ViewModel,
                    MapController = this
                }
            };
            View.Add(_mapView);

            // Leave for now as we may want to add markers using Symbol Manager and this is a useful reference
            /*
            var connection = ViewModel.PointsOfInterest.ToObservableChangeSet();
            connection.Subscribe(OnChanged);
            */
        }
        
        // Leave for now as we may want to add markers using Symbol Manager and this is a useful reference
        /*
        private void OnChanged(IChangeSet<PointOfInterest> changeset)
        {
            
            foreach(var change in changeset)
            {
                if(change.Reason == ListChangeReason.AddRange)
                {
                    var pointAnnotations = new List<MGLPointAnnotation>(change.Range.Count);
                    foreach (var addedPoint in change.Range)
                    {
                        var pointAnnotation = new MGLPointAnnotation()
                        {
                            Title = addedPoint.Name,
                            Subtitle = addedPoint.Category,
                            Coordinate = new CLLocationCoordinate2D(addedPoint.Location.Coordinates.Latitude, addedPoint.Location.Coordinates.Longitude)
                        };
                        pointAnnotations.Add(pointAnnotation);

                        _mapView.AddAnnotations(pointAnnotations.ToArray());
                    }
                }
                else if(change.Reason == ListChangeReason.Add)
                {
                    var pointAnnotation = new MGLPointAnnotation()
                    {
                        Title = change.Item.Current.Name,
                        Subtitle = change.Item.Current.Category,
                        Coordinate = new CLLocationCoordinate2D(change.Item.Current.Location.Coordinates.Latitude, change.Item.Current.Location.Coordinates.Longitude)
                    };
                    _mapView.AddAnnotation(pointAnnotation);
                }
                else if (change.Reason == ListChangeReason.Remove)
                {
                    if(_mapView.Annotations != null && _mapView.Annotations.Any())
                    {
                        var annotation = _mapView.Annotations.FirstOrDefault(a => a.GetTitle() == change.Item.Current.Name);

                        _mapView.RemoveAnnotation(annotation);
                    }
                }
            }
        }
        */
        
        public void OnStyleLoaded(MGLStyle style)
        {
            _style = style;
            
            this.WhenAnyValue(x => x.ViewModel.PointOfInterestFeatures).Subscribe(OnPointsOfInterestChanged);
            this.WhenAnyValue(x => x.ViewModel.VenueFeatures).Subscribe(OnVenuesChanged);
            this.WhenAnyValue(x => x.ViewModel.UserLocation).Subscribe(OnUserLocationChanged);
        }

        public void OnMapLoaded()
        {
        }
        
        private void OnPointsOfInterestChanged(GeoJSON.Net.Feature.FeatureCollection featureCollection)
        {
            UpdatePointsOfInterestGeoSource();
        }

        private void OnVenuesChanged(GeoJSON.Net.Feature.FeatureCollection featureCollection)
        {
            UpdateVenuesGeoSource();
        }

        private void SetupPointsOfInterestGeoSource()
        {
            var data = NSData.FromString(ViewModel.PointOfInterestFeatures.ToGeoJsonFeatureSource());

            var shape = MGLShape.ShapeWithData(data, (int)NSStringEncoding.UTF8, out var error);

            var options = new NSMutableDictionary<NSString, NSObject>();
            //options.Add(MGLShapeSourceOptions.Clustered, NSNumber.FromBoolean(true));
            //options.Add(MGLShapeSourceOptions.ClusterRadius, NSNumber.FromNInt(14));
            //options.Add(MGLShapeSourceOptions.ClusterRadius, NSNumber.FromNInt(50));
            
            var source  = new MGLShapeSource(GEOJSON_POI_SOURCE_ID, shape, new NSDictionary<NSString, NSObject>(options.Keys, options.Values));
            
            if (source != null)
            {
                _pointsOfInterestSource = source;
                
                _style.AddSource(_pointsOfInterestSource);
                
                var layer = new MGLSymbolStyleLayer(identifier: POI_MARKER_LAYER_ID, source: _pointsOfInterestSource);
                // either of these works
                //layer.IconImageName = NSExpression.FromConstant((NSString)MARKER_IMAGE_ID); 
                layer.IconImageName = NSExpression.FromConstant(new NSString(RED_MARKER_IMAGE_ID));
                _style.AddLayer(layer);

                _style.SetImage(UIImage.FromBundle("red_marker"), RED_MARKER_IMAGE_ID);
            }
        }
        
        private void UpdatePointsOfInterestGeoSource()
        {
            if(_pointsOfInterestSource == null)
            {
                SetupPointsOfInterestGeoSource();
            }
            else
            {
                var data = NSData.FromString(ViewModel.PointOfInterestFeatures.ToGeoJsonFeatureSource());

                var shape = MGLShape.ShapeWithData(data, (int)NSStringEncoding.UTF8, out var error);

                _pointsOfInterestSource.Shape = shape;    
            }
        }

        private void SetupVenuesGeoSource()
        {
            var data = NSData.FromString(ViewModel.VenueFeatures.ToGeoJsonFeatureSource());

            var shape = MGLShape.ShapeWithData(data, (int)NSStringEncoding.UTF8, out var error);

            var options = new NSMutableDictionary<NSString, NSObject>();
            //options.Add(MGLShapeSourceOptions.Clustered, NSNumber.FromBoolean(true));
            //options.Add(MGLShapeSourceOptions.ClusterRadius, NSNumber.FromNInt(14));
            //options.Add(MGLShapeSourceOptions.ClusterRadius, NSNumber.FromNInt(50));

            var source = new MGLShapeSource(GEOJSON_VENUE_SOURCE_ID, shape, new NSDictionary<NSString, NSObject>(options.Keys, options.Values));

            if (source != null)
            {
                _venuesSource = source;

                _style.AddSource(_venuesSource);

                var layer = new MGLSymbolStyleLayer(identifier: VENUE_MARKER_LAYER_ID, source: _venuesSource);
                // either of these works
                //layer.IconImageName = NSExpression.FromConstant((NSString)BAR_MARKER_IMAGE_ID); 
                layer.IconImageName = NSExpression.FromConstant(new NSString(BAR_MARKER_IMAGE_ID));
                _style.AddLayer(layer);

                _style.SetImage(UIImage.FromBundle("local_bar"), BAR_MARKER_IMAGE_ID);
            }
        }

        private void UpdateVenuesGeoSource()
        {
            if (_venuesSource == null)
            {
                SetupVenuesGeoSource();
            }
            else
            {
                var data = NSData.FromString(ViewModel.VenueFeatures.ToGeoJsonFeatureSource());

                var shape = MGLShape.ShapeWithData(data, (int)NSStringEncoding.UTF8, out var error);

                _venuesSource.Shape = shape;
            }
        }

        private void OnUserLocationChanged(Position position)
        {
            if (position != null)
            {
                var camera = new MGLMapCamera();
                camera.CenterCoordinate = new CLLocationCoordinate2D(position.Latitude, position.Longitude);
                camera.Altitude = 30000;
                _mapView.SetCamera(camera, 2.0, CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseIn));
            }
        }
    }
    
    public class AppMGLMapViewDelegate : MGLMapViewDelegate
    {
        public IMapControl MapControl { get; set; }
        
        public MapViewModel ViewModel { get; set; }
        
        public IMapController MapController { get; set; }

        public override void MapViewDidFinishLoadingStyle(MGLMapView mapView, MGLStyle style)
        {
            MapController.OnStyleLoaded(style);
        }

        public override void MapViewDidFinishLoadingMap(MGLMapView mapView)
        {
            MapController.OnMapLoaded();
            ViewModel?.OnMapLoaded().ConfigureAwait(false);
        }

        public override void MapViewRegionDidChange(MGLMapView mapView, bool animated)
        {                      
            MapControl.Center = new Position(mapView.CenterCoordinate.Latitude, mapView.CenterCoordinate.Longitude);
        }
    }
}