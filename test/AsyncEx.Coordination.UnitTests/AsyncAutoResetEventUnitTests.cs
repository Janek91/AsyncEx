using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Coordination.UnitTests
{
    public class AsyncAutoResetEventUnitTests
    {
        [Fact]
        public async Task WaitAsync_Unset_IsNotCompleted()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();

            Task task = are.WaitAsync();

            await AsyncAssert.NeverCompletesAsync(task);
        }

        [Fact]
        public void WaitAsync_AfterSet_CompletesSynchronously()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();
            
            are.Set();
            Task task = are.WaitAsync();
            
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void WaitAsync_Set_CompletesSynchronously()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent(true);

            Task task = are.WaitAsync();
            
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task MultipleWaitAsync_AfterSet_OnlyOneIsCompleted()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();

            are.Set();
            Task task1 = are.WaitAsync();
            Task task2 = are.WaitAsync();

            Assert.True(task1.IsCompleted);
            await AsyncAssert.NeverCompletesAsync(task2);
        }

        [Fact]
        public async Task MultipleWaitAsync_Set_OnlyOneIsCompleted()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent(true);

            Task task1 = are.WaitAsync();
            Task task2 = are.WaitAsync();

            Assert.True(task1.IsCompleted);
            await AsyncAssert.NeverCompletesAsync(task2);
        }

        [Fact]
        public async Task MultipleWaitAsync_AfterMultipleSet_OnlyOneIsCompleted()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();

            are.Set();
            are.Set();
            Task task1 = are.WaitAsync();
            Task task2 = are.WaitAsync();

            Assert.True(task1.IsCompleted);
            await AsyncAssert.NeverCompletesAsync(task2);
        }

        [Fact]
        public void WaitAsync_PreCancelled_Set_SynchronouslyCompletesWait()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent(true);
            CancellationToken token = new CancellationToken(true);
            
            Task task = are.WaitAsync(token);

            Assert.True(task.IsCompleted);
            Assert.False(task.IsCanceled);
            Assert.False(task.IsFaulted);
        }

        [Fact]
        public async Task WaitAsync_Cancelled_DoesNotAutoReset()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();
            CancellationTokenSource cts = new CancellationTokenSource();

            cts.Cancel();
            Task task1 = are.WaitAsync(cts.Token);
            task1.WaitWithoutException();
            are.Set();
            Task task2 = are.WaitAsync();

            await task2;
        }

        [Fact]
        public void WaitAsync_PreCancelled_Unset_SynchronouslyCancels()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent(false);
            CancellationToken token = new CancellationToken(true);

            Task task = are.WaitAsync(token);

            Assert.True(task.IsCompleted);
            Assert.True(task.IsCanceled);
            Assert.False(task.IsFaulted);
        }

#if TODO
        [Fact]
        public void WaitAsyncFromCustomSynchronizationContext_PreCancelled_Unset_SynchronouslyCancels()
        {
            AsyncContext.Run(() =>
            {
                var are = new AsyncAutoResetEvent(false);
                var token = new CancellationToken(true);

                var task = are.WaitAsync(token);

                Assert.IsTrue(task.IsCompleted);
                Assert.IsTrue(task.IsCanceled);
                Assert.IsFalse(task.IsFaulted);
            });
        }
#endif

        [Fact]
        public async Task WaitAsync_Cancelled_ThrowsException()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            Task task = are.WaitAsync(cts.Token);
            await AsyncAssert.ThrowsAsync<OperationCanceledException>(task);
        }

        [Fact]
        public void Id_IsNotZero()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();
            Assert.NotEqual(0, are.Id);
        }
    }
}
