using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Coordination.UnitTests
{
    public class AsyncSemaphoreUnitTests
    {
        [Fact]
        public async Task WaitAsync_NoSlotsAvailable_IsNotCompleted()
        {
            AsyncSemaphore semaphore = new AsyncSemaphore(0);
            Assert.Equal(0, semaphore.CurrentCount);
            Task task = semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            await AsyncAssert.NeverCompletesAsync(task);
        }

        [Fact]
        public async Task WaitAsync_SlotAvailable_IsCompleted()
        {
            AsyncSemaphore semaphore = new AsyncSemaphore(1);
            Assert.Equal(1, semaphore.CurrentCount);
            Task task1 = semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            Assert.True(task1.IsCompleted);
            Task task2 = semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            await AsyncAssert.NeverCompletesAsync(task2);
        }

        [Fact]
        public void WaitAsync_PreCancelled_SlotAvailable_SucceedsSynchronously()
        {
            AsyncSemaphore semaphore = new AsyncSemaphore(1);
            Assert.Equal(1, semaphore.CurrentCount);
            CancellationToken token = new CancellationToken(true);

            Task task = semaphore.WaitAsync(token);
            
            Assert.Equal(0, semaphore.CurrentCount);
            Assert.True(task.IsCompleted);
            Assert.False(task.IsCanceled);
            Assert.False(task.IsFaulted);
        }

        [Fact]
        public void WaitAsync_PreCancelled_NoSlotAvailable_CancelsSynchronously()
        {
            AsyncSemaphore semaphore = new AsyncSemaphore(0);
            Assert.Equal(0, semaphore.CurrentCount);
            CancellationToken token = new CancellationToken(true);

            Task task = semaphore.WaitAsync(token);

            Assert.Equal(0, semaphore.CurrentCount);
            Assert.True(task.IsCompleted);
            Assert.True(task.IsCanceled);
            Assert.False(task.IsFaulted);
        }

        [Fact]
        public async Task WaitAsync_Cancelled_DoesNotTakeSlot()
        {
            AsyncSemaphore semaphore = new AsyncSemaphore(0);
            Assert.Equal(0, semaphore.CurrentCount);
            CancellationTokenSource cts = new CancellationTokenSource();
            Task task = semaphore.WaitAsync(cts.Token);
            Assert.Equal(0, semaphore.CurrentCount);
            Assert.False(task.IsCompleted);

            cts.Cancel();

            try { await task; }
            catch (OperationCanceledException) { }
            semaphore.Release();
            Assert.Equal(1, semaphore.CurrentCount);
            Assert.True(task.IsCanceled);
        }

        [Fact]
        public void Release_WithoutWaiters_IncrementsCount()
        {
            AsyncSemaphore semaphore = new AsyncSemaphore(0);
            Assert.Equal(0, semaphore.CurrentCount);
            semaphore.Release();
            Assert.Equal(1, semaphore.CurrentCount);
            Task task = semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task Release_WithWaiters_ReleasesWaiters()
        {
            AsyncSemaphore semaphore = new AsyncSemaphore(0);
            Assert.Equal(0, semaphore.CurrentCount);
            Task task = semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            Assert.False(task.IsCompleted);
            semaphore.Release();
            Assert.Equal(0, semaphore.CurrentCount);
            await task;
        }

        [Fact]
        public void Release_Overflow_ThrowsException()
        {
            AsyncSemaphore semaphore = new AsyncSemaphore(long.MaxValue);
            Assert.Equal(long.MaxValue, semaphore.CurrentCount);
            AsyncAssert.Throws<OverflowException>(() => semaphore.Release());
        }

        [Fact]
        public void Release_ZeroSlots_HasNoEffect()
        {
            AsyncSemaphore semaphore = new AsyncSemaphore(1);
            Assert.Equal(1, semaphore.CurrentCount);
            semaphore.Release(0);
            Assert.Equal(1, semaphore.CurrentCount);
        }

        [Fact]
        public void Id_IsNotZero()
        {
            AsyncSemaphore semaphore = new AsyncSemaphore(0);
            Assert.NotEqual(0, semaphore.Id);
        }
    }
}
