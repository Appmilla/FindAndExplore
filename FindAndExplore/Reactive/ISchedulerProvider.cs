using System.Reactive.Concurrency;

namespace FindAndExplore.Reactive
{
    public interface ISchedulerProvider
    {
        IScheduler MainThread { get; }
        IScheduler ThreadPool { get; }
    }
}
