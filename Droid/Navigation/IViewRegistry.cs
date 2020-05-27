using System;
using FindAndExplore.Reactive;

namespace FindAndExplore.Droid.Navigation
{
    public interface IViewRegistry
    {
        void SetRootViewModel(IViewModel pageViewModel);
    }
}
