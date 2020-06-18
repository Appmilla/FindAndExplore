// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace FindAndExplore.iOS.Presentation
{
	[Register ("ProgressPopup")]
	partial class ProgressPopup
	{
		[Outlet]
		FindAndExplore.iOS.Presentation.AnimationView AnimationViewLoading { get; set; }

		[Outlet]
		UIKit.UILabel LabelProgressText { get; set; }

		[Outlet]
		UIKit.UIView ViewPopupBackground { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (AnimationViewLoading != null) {
				AnimationViewLoading.Dispose ();
				AnimationViewLoading = null;
			}

			if (LabelProgressText != null) {
				LabelProgressText.Dispose ();
				LabelProgressText = null;
			}

			if (ViewPopupBackground != null) {
				ViewPopupBackground.Dispose ();
				ViewPopupBackground = null;
			}
		}
	}
}
