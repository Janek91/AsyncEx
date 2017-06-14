using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Xunit;

namespace AsyncEx.Coordination.UnitTests
{
    public class AsyncMonitorUnitTests
    {
        [Fact]
        public async Task Unlocked_PermitsLock()
        {
            AsyncMonitor monitor = new AsyncMonitor();

            AwaitableDisposable<IDisposable> task = monitor.EnterAsync();
            await task;
        }

        [Fact]
        public async Task Locked_PreventsLockUntilUnlocked()
        {
            AsyncMonitor monitor = new AsyncMonitor();
            TaskCompletionSource<object> task1HasLock = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            TaskCompletionSource<object> task1Continue = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();

            Task task1 = Task.Run(async () =>
            {
                using (await monitor.EnterAsync())
                {
                    task1HasLock.SetResult(null);
                    await task1Continue.Task;
                }
            });
            await task1HasLock.Task;

            Task<IDisposable> lockTask = monitor.EnterAsync().AsTask();
            Assert.False(lockTask.IsCompleted);
            task1Continue.SetResult(null);
            await lockTask;
        }

        [Fact]
        public async Task Pulse_ReleasesOneWaiter()
        {
            AsyncMonitor monitor = new AsyncMonitor();
            int[] completed = { 0 };
            TaskCompletionSource<object> task1Ready = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            TaskCompletionSource<object> task2Ready = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            Task task1 = Task.Run(async () =>
            {
                using (await monitor.EnterAsync())
                {
                    Task waitTask1 = monitor.WaitAsync();
                    task1Ready.SetResult(null);
                    await waitTask1;
                    Interlocked.Increment(ref completed[0]);
                }
            });
            await task1Ready.Task;
            Task task2 = Task.Run(async () =>
            {
                using (await monitor.EnterAsync())
                {
                    Task waitTask2 = monitor.WaitAsync();
                    task2Ready.SetResult(null);
                    await waitTask2;
                    Interlocked.Increment(ref completed[0]);
                }
            });
            await task2Ready.Task;

            using (await monitor.EnterAsync())
            {
                monitor.Pulse();
            }
            await Task.WhenAny(task1, task2).ConfigureAwait(false);
            int result = Interlocked.CompareExchange(ref completed[0], 0, 0);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task PulseAll_ReleasesAllWaiters()
        {
            AsyncMonitor monitor = new AsyncMonitor();
            int[] completed = { 0 };
            TaskCompletionSource<object> task1Ready = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            TaskCompletionSource<object> task2Ready = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            Task waitTask1 = null;
            Task task1 = Task.Run(async () =>
            {
                using (await monitor.EnterAsync())
                {
                    waitTask1 = monitor.WaitAsync();
                    task1Ready.SetResult(null);
                    await waitTask1;
                    Interlocked.Increment(ref completed[0]);
                }
            });
            await task1Ready.Task;
            Task waitTask2 = null;
            Task task2 = Task.Run(async () =>
            {
                using (await monitor.EnterAsync())
                {
                    waitTask2 = monitor.WaitAsync();
                    task2Ready.SetResult(null);
                    await waitTask2;
                    Interlocked.Increment(ref completed[0]);
                }
            });
            await task2Ready.Task;

            AwaitableDisposable<IDisposable> lockTask3 = monitor.EnterAsync();
            using (await lockTask3)
            {
                monitor.PulseAll();
            }
            await Task.WhenAll(task1, task2).ConfigureAwait(false);
            int result = Interlocked.CompareExchange(ref completed[0], 0, 0);

            Assert.Equal(2, result);
        }

        [Fact]
        public void Id_IsNotZero()
        {
            AsyncMonitor monitor = new AsyncMonitor();
            Assert.NotEqual(0, monitor.Id);
        }
    }
}
