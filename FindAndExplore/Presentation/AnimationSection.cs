using System;
namespace FindAndExplore.Presentation
{
    public class AnimationSection
    {
        public string Key { get; }

        public int StartFrame { get; }

        public int EndFrame { get; }

        public AnimationSection(string key, int startFrame, int endFrame)
        {
            Key = key;
            StartFrame = startFrame;
            EndFrame = endFrame;
        }
    }
}
