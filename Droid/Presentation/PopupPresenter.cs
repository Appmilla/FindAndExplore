using System;
using System.Collections.Generic;
using Android.Support.V4.App;
using FindAndExplore.Core.Presentation;

namespace FindAndExplore.Droid.Presentation
{
    public class PopupPresenter : IPopupPresenter
    {
        private readonly FragmentActivity _presentingActivity;
        private ProgressPopup _progressPopup;
        public event EventHandler<AnimationSection> ProgressAnimationCompleted;

        public PopupPresenter(FragmentActivity presentingActivity)
        {
            _presentingActivity = presentingActivity;
        }

        public void DismissProgress() => _presentingActivity.RunOnUiThread(DismissProgressDialog);

        public void ShowProgress(string progressText, string progressHeaderText = null, string json = null,
            IList<AnimationSection> animationSections = null) =>
            _presentingActivity.RunOnUiThread(() => ShowProgressDialog(progressText, progressHeaderText, json, animationSections));

        public void UpdateProgress(string progressText = null, string progressHeaderText = null,
            string animationKey = null) =>
            _presentingActivity.RunOnUiThread(() => UpdateProgressDialog(progressText, progressHeaderText, animationKey));

        private void ShowProgressDialog(string progressText, string progressHeaderText = null, string json = null,
            IList<AnimationSection> animationSections = null)
        {
            if (_progressPopup != null)
            {
                UpdateProgress(progressText, progressHeaderText);
            }
            else
            {
                _progressPopup = ProgressPopup.Instance(progressText, progressHeaderText, json, animationSections);

                var transaction = _presentingActivity.SupportFragmentManager.BeginTransaction();
                transaction.Add(_progressPopup, nameof(ProgressPopup)).CommitAllowingStateLoss();
                _progressPopup.ProgressAnimationCompleted += OnProgressPopupProgressAnimationCompleted;
            }
        }

        private void DismissProgressDialog()
        {
            if (_progressPopup != null)
            {
                var transaction = _presentingActivity.SupportFragmentManager.BeginTransaction();
                transaction.Remove(_progressPopup).CommitAllowingStateLoss();

                _progressPopup.ProgressAnimationCompleted -= OnProgressPopupProgressAnimationCompleted;
                _progressPopup = null;
            }
        }

        private void UpdateProgressDialog(string progressText = null, string progressHeaderText = null,
            string animationKey = null)
        {
            if (_progressPopup == null)
            {
                // if this is called first without a ShowProgress, lets kick that off instead
                ShowProgress(progressText, progressHeaderText);
            }
            else
            {
                _progressPopup.ProgressText = progressText;
                _progressPopup.ProgressHeaderText = progressHeaderText;
                _progressPopup.AnimationKey = animationKey;
            }
        }

        private void OnProgressPopupProgressAnimationCompleted(object sender, AnimationSection e) =>
            ProgressAnimationCompleted?.Invoke(this, e);
    }
}
