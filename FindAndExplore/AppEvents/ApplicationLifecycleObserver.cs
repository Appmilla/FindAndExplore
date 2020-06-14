using System;
using System.Reactive.Subjects;

namespace FindAndExplore.AppEvents
{
    public enum LifecycleEvent
    {
        Started,
        Stopped,
        Paused,
        Resumed
    }

    public interface IApplicationLifecycleObserver
    {
        void SendLifecycleEvent(LifecycleEvent lifecycleEvent);

        IObservable<LifecycleEvent> LifecycleNotifications { get; }
    }

    public class ApplicationLifecycleObserver : IApplicationLifecycleObserver
    {
        readonly Subject<LifecycleEvent> _lifecycleNotifications;

        public IObservable<LifecycleEvent> LifecycleNotifications => _lifecycleNotifications;

        public ApplicationLifecycleObserver()
        {
            _lifecycleNotifications = new Subject<LifecycleEvent>();
        }

        public void SendLifecycleEvent(LifecycleEvent lifecycleEvent)
        {
            _lifecycleNotifications.OnNext(lifecycleEvent);
        }
    }
}
