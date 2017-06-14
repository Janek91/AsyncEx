using System;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Coordination.UnitTests
{
    public class AsyncConditionVariableUnitTests
    {
        [Fact]
        public async Task WaitAsync_WithoutNotify_IsNotCompleted()
        {
            AsyncLock mutex = new AsyncLock();
            AsyncConditionVariable cv = new AsyncConditionVariable(mutex);

            await mutex.LockAsync();
            Task task = cv.WaitAsync();

            await AsyncAssert.NeverCompletesAsync(task).ConfigureAwait(false);
        }

        [Fact]
        public async Task WaitAsync_Notified_IsCompleted()
        {
            AsyncLock mutex = new AsyncLock();
            AsyncConditionVariable cv = new AsyncConditionVariable(mutex);
            await mutex.LockAsync();
            Task task = cv.WaitAsync();

            await Task.Run(async () =>
            {
                using (await mutex.LockAsync())
                {
                    cv.Notify();
                }
            }).ConfigureAwait(false);
            await task;
        }

        [Fact]
        public async Task WaitAsync_AfterNotify_IsNotCompleted()
        {
            AsyncLock mutex = new AsyncLock();
            AsyncConditionVariable cv = new AsyncConditionVariable(mutex);
            await Task.Run(async () =>
            {
                using (await mutex.LockAsync())
                {
                    cv.Notify();
                }
            }).ConfigureAwait(false);

            await mutex.LockAsync();
            Task task = cv.WaitAsync();

            await AsyncAssert.NeverCompletesAsync(task).ConfigureAwait(false);
        }

        [Fact]
        public async Task MultipleWaits_NotifyAll_AllAreCompleted()
        {
            AsyncLock mutex = new AsyncLock();
            AsyncConditionVariable cv = new AsyncConditionVariable(mutex);
            IDisposable key1 = await mutex.LockAsync();
            Task task1 = cv.WaitAsync();
            Task __ = task1.ContinueWith(_ => key1.Dispose());
            IDisposable key2 = await mutex.LockAsync();
            Task task2 = cv.WaitAsync();
            Task ___ = task2.ContinueWith(_ => key2.Dispose());

            await Task.Run(async () =>
            {
                using (await mutex.LockAsync())
                {
                    cv.NotifyAll();
                }
            }).ConfigureAwait(false);

            await task1;
            await task2;
        }

        [Fact]
        public async Task MultipleWaits_Notify_OneIsCompleted()
        {
            AsyncLock mutex = new AsyncLock();
            AsyncConditionVariable cv = new AsyncConditionVariable(mutex);
            IDisposable key = await mutex.LockAsync();
            Task task1 = cv.WaitAsync();
            Task __ = task1.ContinueWith(_ => key.Dispose());
            await mutex.LockAsync();
            Task task2 = cv.WaitAsync();

            await Task.Run(async () =>
            {
                using (await mutex.LockAsync())
                {
                    cv.Notify();
                }
            }).ConfigureAwait(false);

            await task1;
            await AsyncAssert.NeverCompletesAsync(task2).ConfigureAwait(false);
        }

        [Fact]
        public void Id_IsNotZero()
        {
            AsyncLock mutex = new AsyncLock();
            AsyncConditionVariable cv = new AsyncConditionVariable(mutex);
            Assert.NotEqual(0, cv.Id);
        }
    }
}
