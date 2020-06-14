using System.Reactive.Concurrency;
using ReactiveUI;

namespace FindAndExplore.Reactive
{
    public sealed class SchedulerProvider : ISchedulerProvider
    {
        public IScheduler MainThread => RxApp.MainThreadScheduler;
        public IScheduler ThreadPool => Scheduler.Default;
    }
}
