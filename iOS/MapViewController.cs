using Foundation;
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
        }
    }
}