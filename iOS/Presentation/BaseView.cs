using System;
using System.Collections.Generic;
using FindAndExplore.Presentation;
using FindAndExplore.ViewModels;
using ReactiveUI;
using UIKit;

namespace FindAndExplore.iOS.Presentation
{
    public class BaseView<TViewModel> : ReactiveViewController<TViewModel>, IPopupPresenter where TViewModel : BaseViewModel
    {
        public BaseView(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            ViewModel.PopupPresenter = this;
        }

        public event EventHandler<AnimationSection> ProgressAnimationCompleted;

        public void ShowProgress(string progressText, string progressHeaderText = null, string json = null,
            IList<AnimationSection> animationSections = null) => InvokeOnMainThread(() =>
            ShowProgressDialog(progressText, progressHeaderText, json, animationSections));

        private void ShowProgressDialog(string progressText, string progressHeaderText = null, string json = null,
            IList<AnimationSection> animationSections = null)
        {
            var progressPopup = new ProgressPopup(json, animationSections)
            {
                ModalPresentationStyle = UIModalPresentationStyle.OverFullScreen,
                ModalTransitionStyle = UIModalTransitionStyle.CrossDissolve,
                ProgressText = progressText,
                ProgressHeaderText = progressHeaderText
            };

            progressPopup.AnimationCompletionEvent += ProgressPopup_AnimationCompletionEvent;

            PresentViewController(progressPopup, false, null);
        }

        public void UpdateProgress(string progressText = null, string progressHeaderText = null,
           string animationKey = null)
        {
            InvokeOnMainThread(() => UpdateProgressDialog(progressText, progressHeaderText, animationKey));
        }

        private void UpdateProgressDialog(string progressText = null, string progressHeaderText = null,
            string animationKey = null)
        {
            if (PresentedViewController is ProgressPopup progressPopup)
            {
                progressPopup.ProgressText = progressText;
                progressPopup.ProgressHeaderText = progressHeaderText;
                progressPopup.AnimationKey = animationKey;
            }
            else
            {
                // if this is called first without a ShowProgress, lets kick that off instead
                ShowProgress(progressText, progressHeaderText);
            }
        }

        public void DismissProgress()
        {
            InvokeOnMainThread(DismissProgressDialog);
        }

        private void DismissProgressDialog()
        {
            if (PresentedViewController is ProgressPopup progressPopup)
            {
                progressPopup.AnimationCompletionEvent -= ProgressPopup_AnimationCompletionEvent;

                DismissViewController(false, null);
            }
        }

        public void ProgressPopup_AnimationCompletionEvent(object sender, AnimationSection e)
        {
            if (PresentedViewController is ProgressPopup)
            {
                ProgressAnimationCompleted?.Invoke(sender, e);
            }
        }
    }
}
