using System;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Tasks.UnitTests
{
    public class TaskCompletionSourceExtensionsUnitTests
    {
        [Fact]
        public async Task TryCompleteFromCompletedTaskTResult_PropagatesResult()
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            tcs.TryCompleteFromCompletedTask(TaskConstants.Int32NegativeOne);
            int result = await tcs.Task;
            Assert.Equal(-1, result);
        }

        [Fact]
        public async Task TryCompleteFromCompletedTaskTResult_WithDifferentTResult_PropagatesResult()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            tcs.TryCompleteFromCompletedTask(TaskConstants.Int32NegativeOne);
            object result = await tcs.Task;
            Assert.Equal(-1, result);
        }

        [Fact]
        public async Task TryCompleteFromCompletedTaskTResult_PropagatesCancellation()
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            tcs.TryCompleteFromCompletedTask(TaskConstants<int>.Canceled);
            await AsyncAssert.ThrowsAsync<OperationCanceledException>(() => tcs.Task);
        }

        [Fact]
        public async Task TryCompleteFromCompletedTaskTResult_PropagatesException()
        {
            TaskCompletionSource<int> source = new TaskCompletionSource<int>();
            source.TrySetException(new NotImplementedException());

            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            tcs.TryCompleteFromCompletedTask(source.Task);
            await AsyncAssert.ThrowsAsync<NotImplementedException>(() => tcs.Task);
        }

        [Fact]
        public async Task TryCompleteFromCompletedTask_PropagatesResult()
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            tcs.TryCompleteFromCompletedTask(TaskConstants.Completed, () => -1);
            int result = await tcs.Task;
            Assert.Equal(-1, result);
        }

        [Fact]
        public async Task TryCompleteFromCompletedTask_PropagatesCancellation()
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            tcs.TryCompleteFromCompletedTask(TaskConstants.Canceled, () => -1);
            await AsyncAssert.ThrowsAsync<OperationCanceledException>(() => tcs.Task);
        }

        [Fact]
        public async Task TryCompleteFromCompletedTask_PropagatesException()
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            tcs.TryCompleteFromCompletedTask(Task.FromException(new NotImplementedException()), () => -1);
            await AsyncAssert.ThrowsAsync<NotImplementedException>(() => tcs.Task);
        }

        [Fact]
        public async Task CreateAsyncTaskSource_PermitsCompletingTask()
        {
            TaskCompletionSource<object> tcs = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            tcs.SetResult(null);

            await tcs.Task;
        }
    }
}
