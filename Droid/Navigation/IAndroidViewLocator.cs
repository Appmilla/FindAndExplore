using System;
using ReactiveUI;

namespace FindAndExplore.Droid.Navigation
{
    public interface IAndroidViewLocator
    {
        Type ResolveViewType<T>(T viewModel, string contract = null) where T : class;

        IViewFor ResolveModal<T>(T viewModel, string contract = null) where T : class;
    }
}
