using System.Reactive.Concurrency;
using FindAndExplore.Reactive;
using Microsoft.Reactive.Testing;

namespace FindAndExplore.Tests
{
    public sealed class TestSchedulers : ISchedulerProvider
    {
        readonly TestScheduler _testScheduler = new TestScheduler();
        
        IScheduler ISchedulerProvider.MainThread => _testScheduler;
        IScheduler ISchedulerProvider.ThreadPool => _testScheduler;
    }
}