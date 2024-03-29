﻿using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using FindAndExplore.Presentation;

namespace FindAndExplore.Droid.Presentation
{
    public class ProgressPopup : DialogFragment
    {
        public override bool Cancelable => false;

        public string ProgressText
        {
            get => _progressText;
            set
            {
                if (value != null)
                {
                    _progressText = value;
                    if (_progressTextView != null)
                    {
                        _progressTextView.Text = _progressText;
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
                    _animationView.UpdateAnimation(_animationKey, false);
                }
            }
        }

        public event EventHandler<AnimationSection> ProgressAnimationCompleted;

        private string _progressText;
        private string _animationKey;
        private TextView _progressTextView;
        private AnimationView _animationView;

        private readonly string _animationJson;
        private readonly IList<AnimationSection> _animationSections;

        public ProgressPopup(string animationJson, IList<AnimationSection> animationSections)
        {
            _animationJson = animationJson;
            _animationSections = animationSections;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            Dialog.Window.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));

            return inflater.Inflate(Resource.Layout.popup_progress, container);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            _progressTextView = view.FindViewById<TextView>(Resource.Id.LabelProgressMessage);
            _progressTextView.Text = ProgressText;
            _progressTextView.Typeface = Typeface.CreateFromAsset(Android.App.Application.Context.Assets, "MuseoSansRounded_500.otf");
            _progressTextView.TextSize = 16f;

            _animationView = view.FindViewById<AnimationView>(Resource.Id.AnimationViewProgress);
            _animationView.AnimationCompletionEvent += OnAnimationViewAnimationCompletionEvent;
            _animationView.Initalize(_animationJson, _animationSections);

            Cancelable = false;

            if (_animationSections.Any())
            {
                _animationView.Start(_animationSections.First().Key);
            }
            else
            {
                _animationView.Start();
            }
        }

        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var dialog = base.OnCreateDialog(savedInstanceState);
            dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            return dialog;
        }

        public override void OnStart()
        {
            base.OnStart();

            Dialog?.Window.SetLayout(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            _animationView.AnimationCompletionEvent -= OnAnimationViewAnimationCompletionEvent;
        }

        public static ProgressPopup Instance(string progressText = null, string animationJson = null,
            IList<AnimationSection> animationSections = null)
        {
            var progressPopup = new ProgressPopup(animationJson, animationSections)
            {
                ProgressText = progressText
            };

            return progressPopup;
        }

        private void OnAnimationViewAnimationCompletionEvent(object sender, AnimationSection e) =>
            ProgressAnimationCompleted?.Invoke(this, e);
    }
}
