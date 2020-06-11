using System;
using CoreLocation;
using Mapbox;
using ReactiveUI;
using UIKit;

namespace FindAndExplore.iOS
{
    public partial class MapViewController : ReactiveViewController
    {
        private MGLMapView mapView;

        public MapViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = "Map";
            TabBarItem.Image = UIImage.FromBundle("tab_map_icon");

            mapView = new MGLMapView(View.Bounds, MGLStyle.SatelliteStreetsStyleURL)
            {
                Delegate = new AppMGLMapViewDelegate()
            };
            View.Add(mapView);
        }
    }

    public class AppMGLMapViewDelegate : MGLMapViewDelegate
    {
        public override void MapViewDidFinishLoadingMap(MGLMapView mapView)
        {
            mapView.SetCenterCoordinate(new CLLocationCoordinate2D(51.137506, -3.008960), 10, true);
        }
    }
}