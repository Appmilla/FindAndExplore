using CoreLocation;
using Foundation;
using MapboxBindings;
using ReactiveUI;
using System;
using UIKit;

namespace FindAndExplore.iOS
{
    public partial class MapViewController : ReactiveViewController
    {
        public MapViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = "Map";
            TabBarItem.Image = UIImage.FromBundle("tab_map_icon");

            var mapView = new MGLMapView(View.Bounds);
            mapView.SetCenterCoordinate(new CLLocationCoordinate2D(21.028511, 105.804817), 11, false);

            View.Add(mapView);
        }
    }
}