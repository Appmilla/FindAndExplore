using Foundation;
using System;
using UIKit;

namespace FindAndExplore.iOS
{
    public partial class MainTabBarController : UITabBarController
    {
        public MainTabBarController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.TintColor = Colours.Primary;
        }
    }
}