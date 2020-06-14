using System;
using System.Threading;
using System.Threading.Tasks;

namespace FindAndExplore.Threading
{
    /// <summary>
    /// A lock that allows for async based operations and returns a IDisposable which allows for unlocking.
    /// </summary>
    /// <remarks>Straight-up thieved from
    /// http://www.hanselman.com/blog/ComparingTwoTechniquesInNETAsynchronousCoordinationPrimitives.aspx
    /// and all credit to that article.</remarks>
    public sealed class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Task<IDisposable> _releaser;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLock"/> class.
        /// </summary>
        public AsyncLock()
        {
            _releaser = Task.FromResult((IDisposable)new Releaser(this));
        }

        /// <summary>
        /// Performs a lock which will be either released when the cancellation token is cancelled,
        /// or the returned disposable is disposed.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token which allows for release of the lock.</param>
        /// <returns>A disposable which when Disposed will release the lock.</returns>
        public Task<IDisposable> LockAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var wait = _semaphore.WaitAsync(cancellationToken);

            // Happy path. We synchronously acquired the lock.
            if (wait.IsCompleted && !wait.IsFaulted && !wait.IsCanceled)
            {
                return _releaser;
            }

            return wait
                .ContinueWith(
                    (task, state) => task.IsCanceled ? null : (IDisposable)state,
                    _releaser.Result,
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _semaphore?.Dispose();
            _releaser?.Dispose();
        }

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock _toRelease;

            internal Releaser(AsyncLock toRelease)
            {
                _toRelease = toRelease;
            }

            public void Dispose()
            {
                _toRelease._semaphore.Release();
            }
        }
    }
}
