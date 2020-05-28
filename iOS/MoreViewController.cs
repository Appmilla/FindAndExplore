using Foundation;
using ReactiveUI;
using System;
using UIKit;

namespace FindAndExplore.iOS
{
    public partial class MoreViewController : ReactiveViewController
    {
        public MoreViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = "More";
            TabBarItem.Image = UIImage.FromBundle("tab_more_icon");
        }
    }
}