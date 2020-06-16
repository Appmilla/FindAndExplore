using System;
using System.Collections.Generic;

namespace FindAndExplore.Core.Presentation
{
    public interface IPopupPresenter
    {
        event EventHandler<AnimationSection> ProgressAnimationCompleted;

        void ShowProgress(string progressText, string progressHeaderText = null, string json = null, IList<AnimationSection> animationSections = null);

        void UpdateProgress(string progressText = null, string progressHeaderText = null, string animationKey = null);

        void DismissProgress();
    }
}
