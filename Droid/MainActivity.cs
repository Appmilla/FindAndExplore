using Android.App;
using Android.Widget;
using Android.OS;
using ReactiveUI.AndroidSupport;
using Android.Support.Design.Widget;
using AndroidX.Core.Content;
using Android;
using Android.Content.PM;
using AndroidX.Core.App;

namespace FindAndExplore.Droid
{
    [Activity(Label = "FindAndExplore", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : ReactiveAppCompatActivity
    {
        BottomNavigationView _bottomNavigation;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            RequestLocationPermission();

            Com.Mapbox.Mapboxsdk.Mapbox.GetInstance(this, "pk.eyJ1Ijoicndvb2xsY290dCIsImEiOiJja2FnaWlsMHQwNnYyMnpvNWhhbTd1OTRiIn0.5pL3D0LvtE8A6Yuz40RhIA");
            Com.Mapbox.Mapboxsdk.Mapbox.Telemetry.SetDebugLoggingEnabled(true);

            SetContentView(Resource.Layout.Main);

            _bottomNavigation = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation);
            _bottomNavigation.NavigationItemSelected += BottomNavigation_NavigationItemSelected;

            LoadFragment(Resource.Id.menu_map);
        }

        private void RequestLocationPermission()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.AccessFineLocation }, 88);
            }
        }

        private void BottomNavigation_NavigationItemSelected(object sender, BottomNavigationView.NavigationItemSelectedEventArgs e)
        {
            LoadFragment(e.Item.ItemId);
        }

        private void LoadFragment(int id)
        {
            Android.Support.V4.App.Fragment fragment = null;
            switch (id)
            {
                case Resource.Id.menu_map:
                    fragment = new MapFragment();
                    break;
                case Resource.Id.menu_more:
                    fragment = new MoreFragment();
                    break;
            }

            if (fragment == null)
                return;

            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.frameContent, fragment)
                .Commit();
        }
    }
}

