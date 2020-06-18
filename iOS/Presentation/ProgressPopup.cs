using System;
using System.Collections.Generic;
using FindAndExplore.Presentation;
using UIKit;

namespace FindAndExplore.iOS.Presentation
{
    public partial class ProgressPopup : UIViewController
    {
        private const float PanelCornerRadius = 5.0f;

        public string ProgressText
        {
            get => _progressText;
            set
            {
                if (value != null)
                {
                    _progressText = value;
                    if (LabelProgressText != null)
                    {
                        LabelProgressText.Text = _progressText;
                    }
                }
            }
        }

        public string AnimationKey
        {
            get => _animationKey;
            set
            {
                if (value != null)
                {
                    _animationKey = value;
                    UpdateAnimation(_animationKey, false);
                }
            }
        }

        public event EventHandler<AnimationSection> AnimationCompletionEvent;

        private string _animationKey;
        private string _progressText;

        private readonly string _jsonAnimation;
        private readonly IList<AnimationSection> _animationSections;

        public ProgressPopup(string jsonAnimation, IList<AnimationSection> animationSections) 
            : base(nameof(ProgressPopup), null)
        {
            _jsonAnimation = jsonAnimation;
            _animationSections = animationSections;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            ViewPopupBackground.Layer.CornerRadius = PanelCornerRadius;
            LabelProgressText.Text = ProgressText;
            LabelProgressText.Font = UIFont.FromName("MuseoSansRounded-500", 16f);

            AnimationViewLoading.Initialize(_jsonAnimation, _animationSections);
            AnimationViewLoading.AnimationCompletionEvent += AnimationViewLoading_AnimationCompletionEvent;
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            AnimationViewLoading.Stop();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            if (_animationSections == null)
            {
                AnimationViewLoading.Start();
            }
            else
            {
                AnimationViewLoading.Start(_animationSections[0].Key);
            }
        }

        public override void DidReceiveMemoryWarning()
        {
            if (AnimationViewLoading != null)
            {
                AnimationViewLoading.AnimationCompletionEvent -= AnimationViewLoading_AnimationCompletionEvent;
            }

            base.DidReceiveMemoryWarning();
        }

        private void UpdateAnimation(string animationKey, bool loopAnimation) =>
            AnimationViewLoading.UpdateAnimation(animationKey, loopAnimation);

        private void AnimationViewLoading_AnimationCompletionEvent(object sender, AnimationSection e) =>
            AnimationCompletionEvent?.Invoke(this, e);
    }
}