using System.Reactive.Concurrency;

namespace FindAndExplore.Core.Reactive
{
    public interface ISchedulerProvider
    {
        IScheduler MainThread { get; }
        IScheduler ThreadPool { get; }
    }
}
