using Android.App;
using Android.Widget;
using Android.OS;
using ReactiveUI.AndroidSupport;
using Android.Support.Design.Widget;

namespace FindAndExplore.Droid
{
    [Activity(Label = "FindAndExplore", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : ReactiveAppCompatActivity
    {
        BottomNavigationView _bottomNavigation;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Main);

            _bottomNavigation = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation);
            _bottomNavigation.NavigationItemSelected += BottomNavigation_NavigationItemSelected;

            LoadFragment(Resource.Id.menu_map);
        }

        private void BottomNavigation_NavigationItemSelected(object sender, BottomNavigationView.NavigationItemSelectedEventArgs e)
        {
            LoadFragment(e.Item.ItemId);
        }

        void LoadFragment(int id)
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

