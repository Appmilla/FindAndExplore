using System;
using System.Collections.Generic;
using System.Linq;
using CommonServiceLocator;
using CoreLocation;
using DynamicData;
using DynamicData.Binding;
using FindAndExplore.ViewModels;
using FindAndExploreApi.Client;
using GeoJSON.Net.Geometry;
using Mapbox;
using ReactiveUI;
using UIKit;

namespace FindAndExplore.iOS
{
    public partial class MapViewController : ReactiveViewController<MapViewModel>
    {
        private MGLMapView mapView;

        public MapViewController (IntPtr handle) : base (handle)
        {
            ViewModel = ServiceLocator.Current.GetInstance<MapViewModel>();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = "Map";
            TabBarItem.Image = UIImage.FromBundle("tab_map_icon");

            mapView = new MGLMapView(View.Bounds, MGLStyle.SatelliteStreetsStyleURL)
            {
                Delegate = new AppMGLMapViewDelegate() { ViewModel = ViewModel }
            };
            View.Add(mapView);

            var connection = ViewModel.PointsOfInterest.ToObservableChangeSet();
            connection.Subscribe(OnChanged);
        }

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

                        mapView.AddAnnotations(pointAnnotations.ToArray());
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
                    mapView.AddAnnotation(pointAnnotation);
                }
                else if (change.Reason == ListChangeReason.Remove)
                {
                    if(mapView.Annotations != null && mapView.Annotations.Any())
                    {
                        mapView.RemoveAnnotations(mapView.Annotations);
                    }
                }
            }
        }
    }

    public class AppMGLMapViewDelegate : MGLMapViewDelegate
    {
        public MapViewModel ViewModel { get; set; }

        public override void MapViewDidFinishLoadingMap(MGLMapView mapView)
        {
            mapView.SetCenterCoordinate(new CLLocationCoordinate2D(51.137506, -3.008960), 10, true);

            ViewModel?.OnMapLoaded();
        }

        public override void MapViewRegionDidChange(MGLMapView mapView, bool animated)
        {          
            ViewModel.CenterLocation = new Position(mapView.CenterCoordinate.Latitude, mapView.CenterCoordinate.Longitude);
        }
    }
}