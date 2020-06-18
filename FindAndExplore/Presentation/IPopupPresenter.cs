using System;
using System.Collections.Generic;

namespace FindAndExplore.Presentation
{
    public interface IPopupPresenter
    {
        event EventHandler<AnimationSection> ProgressAnimationCompleted;

        void ShowProgress(string progressText, string json = null, IList<AnimationSection> animationSections = null);

        void UpdateProgress(string progressText = null, string animationKey = null);

        void DismissProgress();
    }
}
