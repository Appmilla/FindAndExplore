﻿
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
using Com.Mapbox.Mapboxsdk.Camera;
using Com.Mapbox.Mapboxsdk.Geometry;
using Com.Mapbox.Mapboxsdk.Maps;
using Com.Mapbox.Mapboxsdk.Plugins.Annotation;
using ReactiveUI.AndroidSupport;

namespace FindAndExplore.Droid
{
    public class MapFragment : ReactiveFragment, IOnMapReadyCallback
    {
        MapView mapView;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.MapFragmentView, container, false);

            mapView = view.FindViewById<MapView>(Resource.Id.mapView);
            mapView.OnCreate(savedInstanceState);
            mapView.GetMapAsync(this);

            return view;
        }

        public void OnMapReady(MapboxMap mapboxMap)
        {
            mapboxMap.SetStyle(Style.SATELLITE_STREETS);
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
}
