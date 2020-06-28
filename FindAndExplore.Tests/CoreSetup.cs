using System;
using Akavache;
using Autofac;
using CommonServiceLocator;
using FindAndExplore.Infrastructure;
using FindAndExplore.Reactive;
using NSubstitute;

namespace FindAndExplore.Tests
{
    public class CoreSetup : IDisposable
    {
        public IServiceLocator TheServiceLocator { get; }

        public IContainer Container { get; }
        public ISchedulerProvider SchedulerProvider { get; }
        public IBlobCache BlobCache { get; }
        
        public IErrorReporter ErrorReporter { get; }
        
        public CoreSetup()
        {
            TheServiceLocator = Substitute.For<IServiceLocator>();

            ServiceLocator.SetLocatorProvider(() => TheServiceLocator);

            SchedulerProvider = new TestSchedulers();
            
            ErrorReporter = Substitute.For<IErrorReporter>();
            
            BlobCache = CreateBlobCache();
            
            Container = new ContainerBuilder().Build();
        }
        
        IBlobCache CreateBlobCache()
        {
            Akavache.Registrations.Start("TestRunner");
            return new InMemoryBlobCache(SchedulerProvider.MainThread);
        }

        public virtual void Dispose()
        {
            BlobCache.Dispose();
        }

    }
}