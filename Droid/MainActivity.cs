using Android.App;
using Android.Widget;
using Android.OS;
using ReactiveUI.AndroidSupport;

namespace FindAndExplore.Droid
{
    [Activity(Label = "FindAndExplore", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : ReactiveAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
        }
    }
}

