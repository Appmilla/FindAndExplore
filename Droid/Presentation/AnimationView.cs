using System;
using System.Collections.Generic;
using System.Linq;
using Android.Animation;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Airbnb.Lottie;
using FindAndExplore.Presentation;
using Org.Json;

namespace FindAndExplore.Droid.Presentation
{
    public class AnimationView : FrameLayout, Animator.IAnimatorListener
    {
        public bool IsAnimating => _lottieAnimationView.IsAnimating;

        public event EventHandler<AnimationSection> AnimationCompletionEvent;

        private IList<AnimationSection> _animationSections;
        private AnimationSection _currentAnimationSection;

        private readonly LottieAnimationView _lottieAnimationView;

        public AnimationView(Context context)
            : base(context)
        {
            _lottieAnimationView = new LottieAnimationView(context);
        }

        public AnimationView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            _lottieAnimationView = new LottieAnimationView(context, attrs);
        }

        public void Initalize(string jsonAnimation, IList<AnimationSection> animationSections = null)
        {
            _animationSections = animationSections;
            _lottieAnimationView.SetAnimation(jsonAnimation);
            _lottieAnimationView.AddAnimatorListener(this);

            AddView(_lottieAnimationView);
        }

        public void Start(bool loopAnimation = true)
        {
            try
            {
                _lottieAnimationView.Visibility = ViewStates.Visible;
                _lottieAnimationView.RepeatCount = loopAnimation ? LottieDrawable.Infinite : 0;
                SetAnimationDirection(true);
                _lottieAnimationView.PlayAnimation();
            }
            catch (JSONException ex)
            {
                throw new ArgumentException("The json animation data is invalid", ex);
            }
        }

        public void Start(string animationSectionKey, bool loopAnimation = true)
        {
            CheckAnimationViewAvailable();

            _currentAnimationSection = FindAnimationSection(animationSectionKey);

            CommitAnimation(loopAnimation);
        }

        public void StartReverse(bool loopAnimation = true)
        {
            try
            {
                _lottieAnimationView.Visibility = ViewStates.Visible;
                _lottieAnimationView.RepeatCount = loopAnimation ? LottieDrawable.Infinite : 0;
                SetAnimationDirection(false);
                _lottieAnimationView.PlayAnimation();
            }
            catch (JSONException ex)
            {
                throw new ArgumentException("The json animation data is invalid", ex);
            }
        }

        public void Stop()
        {
            CheckAnimationViewAvailable();

            _lottieAnimationView.PauseAnimation();
        }

        public void UpdateAnimation(string animationSectionKey, bool loopAnimation = true)
        {
            CheckAnimationViewAvailable();

            _currentAnimationSection = FindAnimationSection(animationSectionKey);

            shoudlUpdate = true;
            shoudlUpdateLoopAnimation = loopAnimation;
            //CommitAnimation(loopAnimation);
        }

        private AnimationSection FindAnimationSection(string animationSectionKey)
        {
            var foundAnimationSection = _animationSections.FirstOrDefault(s => s.Key == animationSectionKey);

            if (foundAnimationSection == null)
            {
                Console.WriteLine($"Animation section has not been found: {animationSectionKey}");
                throw new NotImplementedException($"Animation section has not been found: {animationSectionKey}");
            }

            return foundAnimationSection;
        }

        private void CheckAnimationViewAvailable()
        {
            if (_lottieAnimationView == null)
            {
                Console.WriteLine($"Animation view has not been created.  Please make sure you have used the initialize method.");
                throw new NotImplementedException(
                    $"Animation view has not been created.  Please make sure you have used the initialize method.");
            }
        }

        private void CommitAnimation(bool loopAnimation)
        {
            _lottieAnimationView.SetMinAndMaxFrame(_currentAnimationSection.StartFrame, _currentAnimationSection.EndFrame);
            _lottieAnimationView.RepeatCount = loopAnimation ? LottieDrawable.Infinite : 0;
            SetAnimationDirection(true);
            _lottieAnimationView.PlayAnimation();
        }

        private void SetAnimationDirection(bool shouldPlayForward)
        {
            if ((shouldPlayForward && _lottieAnimationView.Speed < 0) || (!shouldPlayForward && _lottieAnimationView.Speed > 0))
            {
                _lottieAnimationView.ReverseAnimationSpeed();
            }
        }

        public void OnAnimationCancel(Animator animation)
        {
            // This needs to be implemented for the Animator.IAnimatorListener interface
        }

        public void OnAnimationEnd(Animator animation)
        {
            AnimationCompletionEvent?.Invoke(this, _currentAnimationSection);
        }

        bool shoudlUpdate;
        bool shoudlUpdateLoopAnimation;

        public void OnAnimationRepeat(Animator animation)
        {
            if (shoudlUpdate)
            {
                shoudlUpdate = false;
                CommitAnimation(shoudlUpdateLoopAnimation);
            }
        }

        public void OnAnimationStart(Animator animation)
        {
            // This needs to be implemented for the Animator.IAnimatorListener interface
        }
    }
}
