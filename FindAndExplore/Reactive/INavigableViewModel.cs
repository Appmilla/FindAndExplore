using System;
namespace FindAndExplore.Reactive
{
    public interface INavigableViewModel
    {
        IObservable<bool> NavigationResult { get; }

        void OnNavigationComplete(bool navigationResult);
    }
}
