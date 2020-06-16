using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Airbnb.Lottie;
using FindAndExplore.Presentation;
using Foundation;
using UIKit;

namespace FindAndExplore.iOS.Presentation
{
    [Register("AnimationView"), DesignTimeVisible(true)]
    public class AnimationView : UIView
    {
        public bool IsAnimating => _lottieAnimationView.IsAnimationPlaying;

        public event EventHandler<AnimationSection> AnimationCompletionEvent;

        private IList<AnimationSection> _animationSections;
        private LOTAnimationView _lottieAnimationView;

        public AnimationView(IntPtr handle)
            : base(handle)
        {
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            if (_lottieAnimationView != null)
            {
                _lottieAnimationView.Frame = Bounds;
            }
            else
            {
                throw new NotImplementedException(
                    $"Animation view has not been created. Please make sure you have used the initialize method.");
            }
        }

        public void Initialize(string jsonAnimation, IList<AnimationSection> animationSections = null)
        {
            try
            {
                _animationSections = animationSections;
                _lottieAnimationView = LOTAnimationView.AnimationNamed(jsonAnimation);
                AddSubview(_lottieAnimationView);
            }
            catch (Exception ex)
            {
            }
        }

        public void Start(bool loopAnimation = true)
        {
            CheckAnimationViewAvailable();

            _lottieAnimationView.LoopAnimation = loopAnimation;
            _lottieAnimationView.Play();
        }

        public void Start(string animationSectionKey, bool loopAnimation = true)
        {
            CheckAnimationViewAvailable();

            var animationSection = FindAnimationSection(animationSectionKey);

            _lottieAnimationView.LoopAnimation = loopAnimation;
            _lottieAnimationView.PlayFromFrame(animationSection.StartFrame, animationSection.EndFrame, null);
        }

        public void StartReverse(bool loopAnimation = true)
        {
            CheckAnimationViewAvailable();

            _lottieAnimationView.LoopAnimation = loopAnimation;
            _lottieAnimationView.PlayFromProgress(1, 0, null);
        }

        public void Stop()
        {
            CheckAnimationViewAvailable();

            _lottieAnimationView.Pause();
        }

        public void UpdateAnimation(string animationSectionKey, bool loopAnimation = true)
        {
            CheckAnimationViewAvailable();

            var animationSection = FindAnimationSection(animationSectionKey);

            _lottieAnimationView.CompletionBlock = (bool animationFinished) =>
            {
                _lottieAnimationView.PlayFromFrame(animationSection.StartFrame, animationSection.EndFrame, (bool completed) =>
                {
                    Console.WriteLine(
                        $"Update animation from frame {animationSection.StartFrame} to frame {animationSection.EndFrame}");
                    _lottieAnimationView.CompletionBlock = null;
                    AnimationCompletionEvent?.Invoke(this, animationSection);
                });
            };

            _lottieAnimationView.LoopAnimation = loopAnimation;
        }

        private AnimationSection FindAnimationSection(string animationSectionKey)
        {
            var foundAnimationSection = _animationSections.FirstOrDefault(s => s.Key == animationSectionKey);
            if (foundAnimationSection == null)
            {
                throw new NotImplementedException($"Animation section has not been found: {animationSectionKey}");
            }

            return foundAnimationSection;
        }

        private void CheckAnimationViewAvailable()
        {
            if (_lottieAnimationView == null)
            {
                throw new NotImplementedException(
                    $"Animation view has not been created.  Please make sure you have used the initialize method.");
            }
        }
    }
}
