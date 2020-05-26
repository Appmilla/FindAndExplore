using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using CoreAnimation;
using FindAndExplore.Reactive;
using ReactiveUI;
using UIKit;

namespace FindAndExplore.iOS.Navigation
{
    [SuppressMessage("Design", "CA1010: Implement generic IEnumerable", Justification = "Base class declared IEnumerable.")]
    public class NavigationViewController : UINavigationController, IView
    {
        private readonly IScheduler _backgroundScheduler;
        private readonly IScheduler _mainScheduler;
        private readonly IViewLocator _viewLocator;

        private readonly Stack<UIViewController> _navigationPages = new Stack<UIViewController>();
        private readonly Subject<IViewModel?> _pagePopped = new Subject<IViewModel?>();

        public NavigationViewController(
            UIViewController rootViewController,
            IScheduler? mainScheduler = null,
            IScheduler? backgroundScheduler = null,
            IViewLocator? viewLocator = null) : base(rootViewController)
        {
            _mainScheduler = mainScheduler ?? RxApp.MainThreadScheduler;
            _backgroundScheduler = backgroundScheduler ?? RxApp.TaskpoolScheduler;
            _viewLocator = viewLocator ?? ReactiveUI.ViewLocator.Current;
        }

        public IScheduler MainThreadScheduler => _mainScheduler;

        public IObservable<IViewModel?> PagePopped => _pagePopped;

        public IObservable<Unit> PopModal() =>
            ModalViewController.DismissViewControllerAsync(true)
            .ToObservable()
            .Select(_ => Unit.Default)
            .ObserveOn(_mainScheduler);

        public IObservable<Unit> PopPage(bool animate = true) =>
            Observable.Create<Unit>(observable =>
            {
                CATransaction.Begin();
                CATransaction.CompletionBlock = () =>
                {
                    observable.OnNext(Unit.Default);
                    observable.OnCompleted();
                };
                PopViewController(animate);
                CATransaction.Commit();
                return Disposable.Empty;
            });

        public IObservable<Unit> PopToRootPage(bool animate = true) =>
            Observable.Create<Unit>(observable =>
            {
                CATransaction.Begin();
                CATransaction.CompletionBlock = () =>
                {
                    observable.OnNext(Unit.Default);
                    observable.OnCompleted();
                };
                PopToRootViewController(true);
                CATransaction.Commit();
                return Disposable.Empty;
            });

        public IObservable<Unit> PushModal(IViewModel modalViewModel, string? contract,
            bool withNavigationPage = true)
        {
            return Observable.Start(
                    () =>
                    {
                        var page = LocatePageFor(modalViewModel, contract);
                        SetPageTitle(page, modalViewModel.Id);
                        return page;
                    },
                    CurrentThreadScheduler.Instance)
                .ObserveOn(CurrentThreadScheduler.Instance)
                .SelectMany(page =>
                {
                    return Observable.Create<Unit>(
                        observer =>
                        {
                            // don't show the page if it's already shown
                            if (ModalViewController is UINavigationController modalNavigationController)
                            {
                                if (modalNavigationController?.TopViewController?.GetType() == page.GetType())
                                {
                                    observer.OnNext(Unit.Default);
                                    observer.OnCompleted();
                                    return Disposable.Empty;
                                }
                            }
                            else if (ModalViewController?.GetType() == page.GetType())
                            {
                                observer.OnNext(Unit.Default);
                                observer.OnCompleted();
                                return Disposable.Empty;
                            }

                            CATransaction.Begin();
                            CATransaction.CompletionBlock = () =>
                            {
                                observer.OnNext(Unit.Default);
                                observer.OnCompleted();
                            };

                            page.ModalPresentationStyle = UIModalPresentationStyle.OverCurrentContext;

                            if (withNavigationPage)
                            {
                                var nav = new UINavigationController(page)
                                {
                                    ModalPresentationStyle = UIModalPresentationStyle.OverFullScreen
                                };

                                var navController = this as UINavigationController;

                                if (PresentedViewController != null)
                                    navController = PresentedViewController as UINavigationController;

                                navController?.PresentViewControllerAsync(nav, true).ToObservable();

                            }
                            else
                            {
                                PresentViewControllerAsync(page, true).ToObservable();
                            }

                            CATransaction.Commit();

                            return Disposable.Empty;
                        });

                });
        }

        public IObservable<Unit> PushPage(
            IViewModel pageViewModel,
            string? contract,
            bool resetStack,
            bool animate = true)
        {
            UIViewController? viewController = null;

            return Observable.Start(
                    () =>
                    {
                        var page = LocatePageFor(pageViewModel, contract);
                        SetPageTitle(page, pageViewModel.Id);
                        viewController = page;
                        return page;
                    },
                    CurrentThreadScheduler.Instance)
                .ObserveOn(CurrentThreadScheduler.Instance)
                .SelectMany(page =>
                {
                    return Observable.Create<Unit>(
                        observer =>
                        {
                            // don't show the page if it's already shown
                            if (TopViewController?.GetType() == page.GetType())
                            {
                                observer.OnNext(Unit.Default);
                                observer.OnCompleted();
                                return Disposable.Empty;
                            }

                            CATransaction.Begin();
                            CATransaction.CompletionBlock = () =>
                            {
                                observer.OnNext(Unit.Default);
                                observer.OnCompleted();
                            };

                            if (resetStack)
                            {
                                CATransaction.Begin();
                                CATransaction.CompletionBlock = () =>
                                {
                                    _navigationPages.Clear();
                                    _navigationPages.Push(this);
                                };

                                SetViewControllers(new UIViewController[] { viewController }, true);
                                CATransaction.Commit();
                            }
                            else
                            {
                                PushViewController(viewController, animate);
                            }

                            CATransaction.Commit();
                            return Disposable.Empty;
                        });
                });
        }

        public override UIViewController PopViewController(bool animated)
        {
            var poppedController = base.PopViewController(animated);

            var view = poppedController as IViewFor;
            _pagePopped.OnNext(view?.ViewModel as IViewModel);

            return poppedController;
        }

        private UIViewController LocatePageFor(object viewModel, string? contract)
        {
            var viewFor = _viewLocator.ResolveView(viewModel, contract);
            var page = viewFor as UIViewController;

            if (viewFor == null)
            {
                throw new InvalidOperationException(
                    $"No view could be located for type '{viewModel.GetType().FullName}', contract '{contract}'. Be sure Splat has an appropriate registration.");
            }

            if (page == null)
            {
                throw new InvalidOperationException(
                    $"Resolved view '{viewFor.GetType().FullName}' for type '{viewModel.GetType().FullName}', contract '{contract}' is not a Page.");
            }

            viewFor.ViewModel = viewModel;

            return page;
        }

        private void SetPageTitle(UIViewController page, string resourceKey)
        {
            page.Title = resourceKey;
        }
    }
}
