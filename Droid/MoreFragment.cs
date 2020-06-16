
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
using CommonServiceLocator;
using FindAndExplore.Droid.Presentation;
using FindAndExplore.ViewModels;
using ReactiveUI.AndroidSupport;

namespace FindAndExplore.Droid
{
    public class MoreFragment : BaseFragment<MoreViewModel>
    {
        public MoreFragment()
        {
            ViewModel = ServiceLocator.Current.GetInstance<MoreViewModel>();
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.more_fragment_view, container, false);
            return view;
        }
    }
}
