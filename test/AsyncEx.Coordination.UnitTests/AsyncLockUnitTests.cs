using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Coordination.UnitTests
{
    public class AsyncLockUnitTests
    {
        [Fact]
        public void AsyncLock_Unlocked_SynchronouslyPermitsLock()
        {
            AsyncLock mutex = new AsyncLock();

            Task<IDisposable> lockTask = mutex.LockAsync().AsTask();

            Assert.True(lockTask.IsCompleted);
            Assert.False(lockTask.IsFaulted);
            Assert.False(lockTask.IsCanceled);
        }

        [Fact]
        public async Task AsyncLock_Locked_PreventsLockUntilUnlocked()
        {
            AsyncLock mutex = new AsyncLock();
            TaskCompletionSource<object> task1HasLock = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            TaskCompletionSource<object> task1Continue = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();

            Task task1 = Task.Run(async () =>
            {
                using (await mutex.LockAsync())
                {
                    task1HasLock.SetResult(null);
                    await task1Continue.Task;
                }
            });
            await task1HasLock.Task;

            Task task2 = Task.Run(async () =>
            {
                await mutex.LockAsync();
            });

            Assert.False(task2.IsCompleted);
            task1Continue.SetResult(null);
            await task2;
        }

        [Fact]
        public async Task AsyncLock_DoubleDispose_OnlyPermitsOneTask()
        {
            AsyncLock mutex = new AsyncLock();
            TaskCompletionSource<object> task1HasLock = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            TaskCompletionSource<object> task1Continue = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();

            await Task.Run(async () =>
            {
                IDisposable key = await mutex.LockAsync();
                key.Dispose();
                key.Dispose();
            });

            Task task1 = Task.Run(async () =>
            {
                using (await mutex.LockAsync())
                {
                    task1HasLock.SetResult(null);
                    await task1Continue.Task;
                }
            });
            await task1HasLock.Task;

            Task task2 = Task.Run(async () =>
            {
                await mutex.LockAsync();
            });

            Assert.False(task2.IsCompleted);
            task1Continue.SetResult(null);
            await task2;
        }

        [Fact]
        public async Task AsyncLock_Locked_OnlyPermitsOneLockerAtATime()
        {
            AsyncLock mutex = new AsyncLock();
            TaskCompletionSource<object> task1HasLock = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            TaskCompletionSource<object> task1Continue = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            TaskCompletionSource<object> task2Ready = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            TaskCompletionSource<object> task2HasLock = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            TaskCompletionSource<object> task2Continue = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();

            Task task1 = Task.Run(async () =>
            {
                using (await mutex.LockAsync())
                {
                    task1HasLock.SetResult(null);
                    await task1Continue.Task;
                }
            });
            await task1HasLock.Task;

            Task task2 = Task.Run(async () =>
            {
                AwaitableDisposable<IDisposable> key = mutex.LockAsync();
                task2Ready.SetResult(null);
                using (await key)
                {
                    task2HasLock.SetResult(null);
                    await task2Continue.Task;
                }
            });
            await task2Ready.Task;

            Task task3 = Task.Run(async () =>
            {
                await mutex.LockAsync();
            });

            task1Continue.SetResult(null);
            await task2HasLock.Task;

            Assert.False(task3.IsCompleted);
            task2Continue.SetResult(null);
            await task2;
            await task3;
        }

        [Fact]
        public void AsyncLock_PreCancelled_Unlocked_SynchronouslyTakesLock()
        {
            AsyncLock mutex = new AsyncLock();
            CancellationToken token = new CancellationToken(true);

            Task<IDisposable> task = mutex.LockAsync(token).AsTask();

            Assert.True(task.IsCompleted);
            Assert.False(task.IsCanceled);
            Assert.False(task.IsFaulted);
        }

        [Fact]
        public void AsyncLock_PreCancelled_Locked_SynchronouslyCancels()
        {
            AsyncLock mutex = new AsyncLock();
            AwaitableDisposable<IDisposable> lockTask = mutex.LockAsync();
            CancellationToken token = new CancellationToken(true);

            Task<IDisposable> task = mutex.LockAsync(token).AsTask();

            Assert.True(task.IsCompleted);
            Assert.True(task.IsCanceled);
            Assert.False(task.IsFaulted);
        }

        [Fact]
        public async Task AsyncLock_CancelledLock_LeavesLockUnlocked()
        {
            AsyncLock mutex = new AsyncLock();
            CancellationTokenSource cts = new CancellationTokenSource();
            TaskCompletionSource<object> taskReady = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();

            IDisposable unlock = await mutex.LockAsync();
            Task task = Task.Run(async () =>
            {
                AwaitableDisposable<IDisposable> lockTask = mutex.LockAsync(cts.Token);
                taskReady.SetResult(null);
                await lockTask;
            });
            await taskReady.Task;
            cts.Cancel();
            await AsyncAssert.ThrowsAsync<OperationCanceledException>(task);
            Assert.True(task.IsCanceled);
            unlock.Dispose();

            AwaitableDisposable<IDisposable> finalLockTask = mutex.LockAsync();
            await finalLockTask;
        }

        [Fact]
        public async Task AsyncLock_CanceledLock_ThrowsException()
        {
            AsyncLock mutex = new AsyncLock();
            CancellationTokenSource cts = new CancellationTokenSource();

            await mutex.LockAsync();
            Task<IDisposable> canceledLockTask = mutex.LockAsync(cts.Token).AsTask();
            cts.Cancel();

            await AsyncAssert.ThrowsAsync<OperationCanceledException>(canceledLockTask);
        }

        [Fact]
        public async Task AsyncLock_CanceledTooLate_StillTakesLock()
        {
            AsyncLock mutex = new AsyncLock();
            CancellationTokenSource cts = new CancellationTokenSource();

            AwaitableDisposable<IDisposable> cancelableLockTask;
            using (await mutex.LockAsync())
            {
                cancelableLockTask = mutex.LockAsync(cts.Token);
            }

            IDisposable key = await cancelableLockTask;
            cts.Cancel();

            Task<IDisposable> nextLocker = mutex.LockAsync().AsTask();
            Assert.False(nextLocker.IsCompleted);

            key.Dispose();
            await nextLocker;
        }

        [Fact]
        public void Id_IsNotZero()
        {
            AsyncLock mutex = new AsyncLock();
            Assert.NotEqual(0, mutex.Id);
        }

        [Fact]
        public async Task AsyncLock_SupportsMultipleAsynchronousLocks()
        {
            // This test deadlocks with the old AsyncEx: https://github.com/StephenCleary/AsyncEx/issues/57

            await Task.Run(() =>
            {
                AsyncLock asyncLock = new AsyncLock();
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;
                Task task1 = Task.Run(
                    async () =>
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            using (await asyncLock.LockAsync())
                            {
                                Thread.Sleep(10);
                            }
                        }
                    });
                Task task2 = Task.Run(
                    () =>
                    {
                        using (asyncLock.Lock())
                        {
                            Thread.Sleep(1000);
                        }
                    });

                task2.Wait();
                cancellationTokenSource.Cancel();
                task1.Wait();
            });
        }
    }
}
