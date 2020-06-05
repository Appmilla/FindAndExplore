
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Mapbox.Mapboxsdk.Maps;
using ReactiveUI.AndroidSupport;

namespace FindAndExplore.Droid
{
    public class MapFragment : ReactiveFragment
    {
        MapView mapView;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.MapFragmentView, container, false);

            mapView = view.FindViewById<MapView>(Resource.Id.mapView);
            mapView.OnCreate(savedInstanceState);
            mapView.GetMapAsync(new OnMapReadyCallback());

            return view;
        }

        public override void OnStart()
        {
            base.OnStart();
            mapView.OnStart();
        }
        public override void OnResume()
        {
            base.OnResume();
            mapView.OnResume();
        }
        public override void OnPause()
        {
            mapView.OnPause();
            base.OnPause();
        }
        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            mapView.OnSaveInstanceState(outState);
        }
        public override void OnStop()
        {
            base.OnStop();
            mapView.OnStop();
        }
        public override void OnDestroyView()
        {
            mapView.OnDestroy();
            base.OnDestroy();
        }
        public override void OnLowMemory()
        {
            base.OnLowMemory();
            mapView.OnLowMemory();
        }
    }

    public class OnMapReadyCallback : Java.Lang.Object, IOnMapReadyCallback
    {
        public void OnMapReady(MapboxMap mapboxMap)
        {
            mapboxMap.SetStyle(Style.SatelliteStreets);
        }
    }
}
