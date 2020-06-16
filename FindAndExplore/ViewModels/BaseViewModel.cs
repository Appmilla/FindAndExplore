using System;
using FindAndExplore.Presentation;
using ReactiveUI;

namespace FindAndExplore.ViewModels
{
    public class BaseViewModel : ReactiveObject
    {
        public IPopupPresenter PopupPresenter { get; set; }
    }
}
