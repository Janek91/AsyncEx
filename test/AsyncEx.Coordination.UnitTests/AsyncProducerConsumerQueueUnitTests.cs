using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Coordination.UnitTests
{
    public class AsyncProducerConsumerQueueUnitTests
    {
        [Fact]
        public void ConstructorWithZeroMaxCount_Throws()
        {
            AsyncAssert.Throws<ArgumentOutOfRangeException>(() => new AsyncProducerConsumerQueue<int>(0));
        }

        [Fact]
        public void ConstructorWithZeroMaxCountAndCollection_Throws()
        {
            AsyncAssert.Throws<ArgumentOutOfRangeException>(() => new AsyncProducerConsumerQueue<int>(new int[0], 0));
        }

        [Fact]
        public void ConstructorWithMaxCountSmallerThanCollectionCount_Throws()
        {
            AsyncAssert.Throws<ArgumentException>(() => new AsyncProducerConsumerQueue<int>(new[] { 3, 5 }, 1));
        }

        [Fact]
        public async Task ConstructorWithCollection_AddsItems()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>(new[] { 3, 5, 7 });

            int result1 = await queue.DequeueAsync().ConfigureAwait(false);
            int result2 = await queue.DequeueAsync().ConfigureAwait(false);
            int result3 = await queue.DequeueAsync().ConfigureAwait(false);

            Assert.Equal(3, result1);
            Assert.Equal(5, result2);
            Assert.Equal(7, result3);
        }

        [Fact]
        public async Task EnqueueAsync_SpaceAvailable_EnqueuesItem()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>();

            await queue.EnqueueAsync(3).ConfigureAwait(false);
            int result = await queue.DequeueAsync().ConfigureAwait(false);

            Assert.Equal(3, result);
        }

        [Fact]
        public async Task EnqueueAsync_CompleteAdding_ThrowsException()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>();
            queue.CompleteAdding();

            await AsyncAssert.ThrowsAsync<InvalidOperationException>(() => queue.EnqueueAsync(3)).ConfigureAwait(false);
        }

        [Fact]
        public async Task DequeueAsync_EmptyAndComplete_ThrowsException()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>();
            queue.CompleteAdding();

            await AsyncAssert.ThrowsAsync<InvalidOperationException>(() => queue.DequeueAsync()).ConfigureAwait(false);
        }

        [Fact]
        public async Task DequeueAsync_Empty_DoesNotComplete()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>();

            Task<int> task = queue.DequeueAsync();

            await AsyncAssert.NeverCompletesAsync(task).ConfigureAwait(false);
        }

        [Fact]
        public async Task DequeueAsync_Empty_ItemAdded_Completes()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>();
            Task<int> task = queue.DequeueAsync();

            await queue.EnqueueAsync(13).ConfigureAwait(false);
            int result = await task;

            Assert.Equal(13, result);
        }

        [Fact]
        public async Task DequeueAsync_Cancelled_Throws()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>();
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<int> task = queue.DequeueAsync(cts.Token);

            cts.Cancel();

            await AsyncAssert.ThrowsAsync<OperationCanceledException>(() => task).ConfigureAwait(false);
        }

        [Fact]
        public async Task EnqueueAsync_Full_DoesNotComplete()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>(new[] { 13 }, 1);

            Task task = queue.EnqueueAsync(7);

            await AsyncAssert.NeverCompletesAsync(task).ConfigureAwait(false);
        }

        [Fact]
        public async Task EnqueueAsync_SpaceAvailable_Completes()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>(new[] { 13 }, 1);
            Task task = queue.EnqueueAsync(7);

            await queue.DequeueAsync().ConfigureAwait(false);

            await task;
        }

        [Fact]
        public async Task EnqueueAsync_Cancelled_Throws()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>(new[] { 13 }, 1);
            CancellationTokenSource cts = new CancellationTokenSource();
            Task task = queue.EnqueueAsync(7, cts.Token);

            cts.Cancel();

            await AsyncAssert.ThrowsAsync<OperationCanceledException>(() => task).ConfigureAwait(false);
        }

        [Fact]
        public void CompleteAdding_MultipleTimes_DoesNotThrow()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>();
            queue.CompleteAdding();

            queue.CompleteAdding();
        }

        [Fact]
        public async Task OutputAvailableAsync_NoItemsInQueue_IsNotCompleted()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>();

            Task<bool> task = queue.OutputAvailableAsync();

            await AsyncAssert.NeverCompletesAsync(task).ConfigureAwait(false);
        }

        [Fact]
        public async Task OutputAvailableAsync_ItemInQueue_ReturnsTrue()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>();
            queue.Enqueue(13);

            bool result = await queue.OutputAvailableAsync().ConfigureAwait(false);
            Assert.True(result);
        }

        [Fact]
        public async Task OutputAvailableAsync_NoItemsAndCompleted_ReturnsFalse()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>();
            queue.CompleteAdding();

            bool result = await queue.OutputAvailableAsync().ConfigureAwait(false);
            Assert.False(result);
        }

        [Fact]
        public async Task OutputAvailableAsync_ItemInQueueAndCompleted_ReturnsTrue()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>();
            queue.Enqueue(13);
            queue.CompleteAdding();

            bool result = await queue.OutputAvailableAsync().ConfigureAwait(false);
            Assert.True(result);
        }

        [Fact]
        public async Task StandardAsyncSingleConsumerCode()
        {
            AsyncProducerConsumerQueue<int> queue = new AsyncProducerConsumerQueue<int>();
            Task producer = Task.Run(() =>
            {
                queue.Enqueue(3);
                queue.Enqueue(13);
                queue.Enqueue(17);
                queue.CompleteAdding();
            });

            List<int> results = new List<int>();
            while (await queue.OutputAvailableAsync().ConfigureAwait(false))
            {
                results.Add(queue.Dequeue());
            }

            Assert.Equal(results.Count, 3);
            Assert.Equal(results[0], 3);
            Assert.Equal(results[1], 13);
            Assert.Equal(results[2], 17);
        }
    }
}
