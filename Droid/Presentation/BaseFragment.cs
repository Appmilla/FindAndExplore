using System;
using System.Collections.Generic;
using Android.Content;
using FindAndExplore.ViewModels;
using ReactiveUI.AndroidSupport;

namespace FindAndExplore.Droid.Presentation
{
    public class BaseFragment<TViewModel> : ReactiveFragment<TViewModel> where TViewModel : BaseViewModel
    {
        public override void OnAttach(Context context)
        {
            base.OnAttach(context);

            ViewModel.PopupPresenter = new PopupPresenter(Activity);
        }
    }
}
