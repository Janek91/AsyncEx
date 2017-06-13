using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Coordination.UnitTests
{
    public class AsyncWaitQueueUnitTests
    {
        [Fact]
        public void IsEmpty_WhenEmpty_IsTrue()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WithOneItem_IsFalse()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            queue.Enqueue();
            Assert.False(queue.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WithTwoItems_IsFalse()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            queue.Enqueue();
            queue.Enqueue();
            Assert.False(queue.IsEmpty);
        }

        [Fact]
        public void Dequeue_SynchronouslyCompletesTask()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            Task<object> task = queue.Enqueue();
            queue.Dequeue();
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task Dequeue_WithTwoItems_OnlyCompletesFirstItem()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            Task<object> task1 = queue.Enqueue();
            Task<object> task2 = queue.Enqueue();
            queue.Dequeue();
            Assert.True(task1.IsCompleted);
            await AsyncAssert.NeverCompletesAsync(task2);
        }

        [Fact]
        public void Dequeue_WithResult_SynchronouslyCompletesWithResult()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            object result = new object();
            Task<object> task = queue.Enqueue();
            queue.Dequeue(result);
            Assert.Same(result, task.Result);
        }

        [Fact]
        public void Dequeue_WithoutResult_SynchronouslyCompletesWithDefaultResult()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            Task<object> task = queue.Enqueue();
            queue.Dequeue();
            Assert.Equal(default(object), task.Result);
        }

        [Fact]
        public void DequeueAll_SynchronouslyCompletesAllTasks()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            Task<object> task1 = queue.Enqueue();
            Task<object> task2 = queue.Enqueue();
            queue.DequeueAll();
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
        }

        [Fact]
        public void DequeueAll_WithoutResult_SynchronouslyCompletesAllTasksWithDefaultResult()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            Task<object> task1 = queue.Enqueue();
            Task<object> task2 = queue.Enqueue();
            queue.DequeueAll();
            Assert.Equal(default(object), task1.Result);
            Assert.Equal(default(object), task2.Result);
        }

        [Fact]
        public void DequeueAll_WithResult_CompletesAllTasksWithResult()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            object result = new object();
            Task<object> task1 = queue.Enqueue();
            Task<object> task2 = queue.Enqueue();
            queue.DequeueAll(result);
            Assert.Same(result, task1.Result);
            Assert.Same(result, task2.Result);
        }

        [Fact]
        public void TryCancel_EntryFound_SynchronouslyCancelsTask()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            Task<object> task = queue.Enqueue();
            queue.TryCancel(task, new CancellationToken(true));
            Assert.True(task.IsCanceled);
        }

        [Fact]
        public void TryCancel_EntryFound_RemovesTaskFromQueue()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            Task<object> task = queue.Enqueue();
            queue.TryCancel(task, new CancellationToken(true));
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public void TryCancel_EntryNotFound_DoesNotRemoveTaskFromQueue()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            Task<object> task = queue.Enqueue();
            queue.Enqueue();
            queue.Dequeue();
            queue.TryCancel(task, new CancellationToken(true));
            Assert.False(queue.IsEmpty);
        }

        [Fact]
        public async Task Cancelled_WhenInQueue_CancelsTask()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<object> task = queue.Enqueue(new object(), cts.Token);
            cts.Cancel();
            await AsyncAssert.ThrowsAsync<OperationCanceledException>(task);
        }

        [Fact]
        public async Task Cancelled_WhenInQueue_RemovesTaskFromQueue()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<object> task = queue.Enqueue(new object(), cts.Token);
            cts.Cancel();
            await AsyncAssert.ThrowsAsync<OperationCanceledException>(task);
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public void Cancelled_WhenNotInQueue_DoesNotRemoveTaskFromQueue()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<object> task = queue.Enqueue(new object(), cts.Token);
            Task<object> _ = queue.Enqueue();
            queue.Dequeue();
            cts.Cancel();
            Assert.False(queue.IsEmpty);
        }

        [Fact]
        public void Cancelled_BeforeEnqueue_SynchronouslyCancelsTask()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            Task<object> task = queue.Enqueue(new object(), cts.Token);
            Assert.True(task.IsCanceled);
        }

        [Fact]
        public void Cancelled_BeforeEnqueue_RemovesTaskFromQueue()
        {
            IAsyncWaitQueue<object> queue = new DefaultAsyncWaitQueue<object>() as IAsyncWaitQueue<object>;
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            Task<object> task = queue.Enqueue(new object(), cts.Token);
            Assert.True(queue.IsEmpty);
        }
    }
}
