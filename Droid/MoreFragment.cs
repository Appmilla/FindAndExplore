
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
    public class MoreFragment : ReactiveFragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.MoreFragmentView, container, false);
            return view;
        }
    }
}
