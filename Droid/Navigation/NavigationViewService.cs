using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Android.Content;
using Android.Support.V4.App;
using Android.Support.V7.App;
using FindAndExplore.Reactive;
using ReactiveUI;
using ReactiveUI.AndroidSupport;
using CommonServiceLocator;
using System.Reactive.Threading.Tasks;
using Plugin.CurrentActivity;

namespace FindAndExplore.Droid.Navigation
{
    public class NavigationViewService : IView, IViewRegistry
    {
        private readonly IScheduler _backgroundScheduler;
        private readonly IScheduler _mainScheduler;
        private readonly IAndroidViewLocator _viewLocator;

        private readonly Stack<AppCompatActivity> _navigationPages = new Stack<AppCompatActivity>();
        private readonly Subject<IViewModel?> _pagePopped = new Subject<IViewModel?>();

        private readonly Stack<ReactiveDialogFragment> _modalPages = new Stack<ReactiveDialogFragment>();

        static string _dialogTag = "fullscreen_dialog";
        IViewModel RootViewModel { get; set; }

        public NavigationViewService(
            IScheduler? mainScheduler = null,
            IScheduler? backgroundScheduler = null,
            IAndroidViewLocator? viewLocator = null)
        {
            _mainScheduler = mainScheduler ?? RxApp.MainThreadScheduler;
            _backgroundScheduler = backgroundScheduler ?? RxApp.TaskpoolScheduler;
            _viewLocator = viewLocator ?? ServiceLocator.Current.GetInstance<IAndroidViewLocator>();
        }

        public IScheduler MainThreadScheduler => _mainScheduler;

        public IObservable<IViewModel?> PagePopped => _pagePopped;

        public IObservable<Unit> PopModal() =>
            Observable.Create<Unit>(observable =>
            {
                if (_modalPages.Count <= 0) return Disposable.Empty;

                var topModal = _modalPages.Pop();
                topModal?.Dismiss();

                var view = topModal as IViewFor;
                _pagePopped.OnNext(view?.ViewModel as IViewModel);

                observable.OnNext(Unit.Default);
                observable.OnCompleted();

                return Disposable.Empty;
            });

        public IObservable<Unit> PopPage(bool animate = true) =>
            Observable.Create<Unit>(observable =>
            {
                PopView(animate);

                observable.OnNext(Unit.Default);
                observable.OnCompleted();

                return Disposable.Empty;
            });

        public IObservable<Unit> PopToRootPage(bool animate = true)
        {
            return Observable.Start(
                    () =>
                    {
                        var page = LocatePageFor(RootViewModel, string.Empty);

                        return page;
                    },
                    CurrentThreadScheduler.Instance)
                .ObserveOn(CurrentThreadScheduler.Instance)
                .SelectMany(page =>
                {
                    return Observable.Create<Unit>(async observer =>
                    {
                        var intent = new Intent(CrossCurrentActivity.Current.Activity, page);

                        intent.SetFlags(ActivityFlags.ClearTop);

                        var currentActivity = CrossCurrentActivity.Current.Activity;

                        if (RootViewModel is INavigableViewModel navigableViewModel)
                        {
                            await StartActivityAsync(intent, navigableViewModel);
                        }
                        else
                        {
                            CrossCurrentActivity.Current.Activity.StartActivity(intent);
                        }

                        if (animate)
                            currentActivity?.FinishAfterTransition();
                        else
                            currentActivity?.Finish();

                        while (_navigationPages.Count > 0)
                        {
                            var poppedActivity = _navigationPages.Pop();
                            var view = poppedActivity as IViewFor;
                            _pagePopped.OnNext(view?.ViewModel as IViewModel);
                        }

                        _navigationPages.Push(CrossCurrentActivity.Current.Activity as AppCompatActivity);
                        observer.OnNext(Unit.Default);
                        observer.OnCompleted();

                        return Disposable.Empty;

                    }).SubscribeOn(RxApp.MainThreadScheduler);
                });
        }

        public IObservable<Unit> PushModal(IViewModel modalViewModel, string? contract, bool withNavigationPage = true)
        {
            return Observable.Start(
                    () =>
                    {
                        var modal = LocateModalFor(modalViewModel, contract);
                        return modal;
                    },
                    CurrentThreadScheduler.Instance)
                .ObserveOn(CurrentThreadScheduler.Instance)
                .SelectMany(modal => {
                    return Observable.Create<Unit>(async observer =>
                    {
                        // don't show the page if it's already shown
                        var fragmentManager = ((FragmentActivity)CrossCurrentActivity.Current.Activity).SupportFragmentManager;
                        var currentDialogFragment = fragmentManager.FindFragmentByTag(_dialogTag);
                        if (currentDialogFragment != null)
                        {
                            if (currentDialogFragment.GetType() == modal.GetType())
                            {
                                observer.OnNext(Unit.Default);
                                observer.OnCompleted();
                                return Disposable.Empty;
                            }
                        }

                        if (modalViewModel is INavigableViewModel navigableViewModel)
                        {
                            await ShowDialogAsync(modal, navigableViewModel);
                        }
                        else
                        {
                            observer.OnError(new Exception("ViewModel must implement INavigableViewModel"));
                        }

                        observer.OnNext(Unit.Default);
                        observer.OnCompleted();

                        return Disposable.Empty;

                    }).SubscribeOn(RxApp.MainThreadScheduler);
                });
        }

        Task<bool> ShowDialogAsync(ReactiveDialogFragment dialogFragment, INavigableViewModel navigableViewModel)
        {
            var ret = navigableViewModel.NavigationResult
                .Select(x => x)
                .FirstAsync()
                .ToTask();

            var fragmentManager = ((FragmentActivity)CrossCurrentActivity.Current.Activity).SupportFragmentManager;
            var ft = fragmentManager.BeginTransaction();
            var prev = fragmentManager.FindFragmentByTag(_dialogTag);
            if (prev == null)
            {
                ft.SetTransition((int)Android.App.FragmentTransit.FragmentOpen);
                ft.AddToBackStack(null);

                dialogFragment.Show(ft, _dialogTag);

                _modalPages.Push(dialogFragment);
            }
            return ret;
        }

        Task<bool> StartActivityAsync(Intent intent, INavigableViewModel navigableViewModel)
        {
            var ret = navigableViewModel.NavigationResult
                .Select(x => x)
                .FirstAsync()
                .ToTask();

            CrossCurrentActivity.Current.Activity.StartActivity(intent);

            return ret;
        }

        public IObservable<Unit> PushPage(
            IViewModel pageViewModel,
            string? contract,
            bool resetStack,
            bool animate = true)
        {
            return Observable.Start(
                    () =>
                    {
                        var page = LocatePageFor(pageViewModel, contract);

                        return page;
                    },
                    CurrentThreadScheduler.Instance)
                .ObserveOn(CurrentThreadScheduler.Instance)
                .SelectMany(page =>
                {
                    return Observable.Create<Unit>(async observer =>
                    {
                        // don't show the page if it's already shown
                        if (CrossCurrentActivity.Current.Activity.GetType() == page)
                        {
                            observer.OnNext(Unit.Default);
                            observer.OnCompleted();
                            return Disposable.Empty;
                        }

                        var intent = new Intent(CrossCurrentActivity.Current.Activity, page);

                        if (resetStack)
                        {
                            _navigationPages.Clear();
                            intent.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                        }

                        if (pageViewModel is INavigableViewModel navigableViewModel)
                        {
                            await StartActivityAsync(intent, navigableViewModel);
                        }
                        else
                        {
                            CrossCurrentActivity.Current.Activity.StartActivity(intent);
                        }

                        _navigationPages.Push(CrossCurrentActivity.Current.Activity as AppCompatActivity);
                        observer.OnNext(Unit.Default);
                        observer.OnCompleted();

                        return Disposable.Empty;

                    }).SubscribeOn(RxApp.MainThreadScheduler);
                });
        }

        public Type ResolveViewType<T>(T viewModel, string contract = null) where T : class
        {
            var viewType = _viewLocator.ResolveViewType<T>(viewModel, contract);
            if (viewType != null)
                return viewType;

            throw new ArgumentException("Unable to resolve View");
        }

        public void SetRootViewModel(IViewModel pageViewModel)
        {
            RootViewModel = pageViewModel;
        }

        private AppCompatActivity PopView(bool animated)
        {
            if (!_navigationPages.Any())
                throw new Exception("No pages on navigation stack, nothing to pop");

            if (animated)
                CrossCurrentActivity.Current.Activity?.FinishAfterTransition();
            else
                CrossCurrentActivity.Current.Activity?.Finish();

            var poppedActivity = _navigationPages.Pop();
            var view = poppedActivity as IViewFor;
            _pagePopped.OnNext(view?.ViewModel as IViewModel);

            return poppedActivity;
        }

        Type LocatePageFor(IViewModel viewModel, string? contract)
        {
            var viewFor = ResolveViewType(viewModel, contract);

            if (viewFor == null)
            {
                throw new InvalidOperationException(
                    $"No view could be located for type '{viewModel.GetType().FullName}', contract '{contract}'. Be sure Splat has an appropriate registration.");
            }

            return viewFor;
        }

        ReactiveDialogFragment LocateModalFor(IViewModel viewModel, string? contract)
        {
            var viewFor = _viewLocator.ResolveModal(viewModel, contract);

            if (viewFor == null)
            {
                throw new InvalidOperationException(
                    $"The viewmodel must implement IViewFor '{viewModel.GetType().FullName}', contract '{contract}'.");
            }
            viewFor.ViewModel = viewModel;

            var modal = viewFor as ReactiveDialogFragment;

            return modal;
        }
    }
}
