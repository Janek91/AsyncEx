using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Tasks.UnitTests
{
    public class TaskExtensionsUnitTests
    {
        [Fact]
        public void WaitAsyncTResult_TokenThatCannotCancel_ReturnsSourceTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            Task<object> task = tcs.Task.WaitAsync(CancellationToken.None);

            Assert.Same(tcs.Task, task);
        }

        [Fact]
        public void WaitAsyncTResult_AlreadyCanceledToken_ReturnsSynchronouslyCanceledTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            CancellationToken token = new CancellationToken(true);
            Task<object> task = tcs.Task.WaitAsync(token);

            Assert.True(task.IsCanceled);
            Assert.Equal(token, GetCancellationTokenFromTask(task));
        }

        [Fact]
        public async Task WaitAsyncTResult_TokenCanceled_CancelsTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<object> task = tcs.Task.WaitAsync(cts.Token);
            Assert.False(task.IsCompleted);

            cts.Cancel();

            await AsyncAssert.ThrowsAsync<OperationCanceledException>(task).ConfigureAwait(false);
            Assert.Equal(cts.Token, GetCancellationTokenFromTask(task));
        }

        [Fact]
        public void WaitAsync_TokenThatCannotCancel_ReturnsSourceTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            Task task = ((Task)tcs.Task).WaitAsync(CancellationToken.None);

            Assert.Same(tcs.Task, task);
        }

        [Fact]
        public void WaitAsync_AlreadyCanceledToken_ReturnsSynchronouslyCanceledTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            CancellationToken token = new CancellationToken(true);
            Task task = ((Task)tcs.Task).WaitAsync(token);

            Assert.True(task.IsCanceled);
            Assert.Equal(token, GetCancellationTokenFromTask(task));
        }

        [Fact]
        public async Task WaitAsync_TokenCanceled_CancelsTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            CancellationTokenSource cts = new CancellationTokenSource();
            Task task = ((Task)tcs.Task).WaitAsync(cts.Token);
            Assert.False(task.IsCompleted);

            cts.Cancel();

            await AsyncAssert.ThrowsAsync<OperationCanceledException>(task).ConfigureAwait(false);
            Assert.Equal(cts.Token, GetCancellationTokenFromTask(task));
        }

        [Fact]
        public void WhenAnyTResult_AlreadyCanceledToken_ReturnsSynchronouslyCanceledTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            CancellationToken token = new CancellationToken(true);
            Task<Task<object>> task = new[] { tcs.Task }.WhenAny(token);

            Assert.True(task.IsCanceled);
            Assert.Equal(token, GetCancellationTokenFromTask(task));
        }

        [Fact]
        public async Task WhenAnyTResult_TaskCompletes_CompletesTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<Task<object>> task = new[] { tcs.Task }.WhenAny(cts.Token);
            Assert.False(task.IsCompleted);

            tcs.SetResult(null);

            Task<object> result = await task;
            Assert.Same(tcs.Task, result);
        }

        [Fact]
        public async Task WhenAnyTResult_TokenCanceled_CancelsTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<Task<object>> task = new[] { tcs.Task }.WhenAny(cts.Token);
            Assert.False(task.IsCompleted);

            cts.Cancel();

            await AsyncAssert.ThrowsAsync<OperationCanceledException>(task).ConfigureAwait(false);
            Assert.Equal(cts.Token, GetCancellationTokenFromTask(task));
        }

        [Fact]
        public void WhenAny_AlreadyCanceledToken_ReturnsSynchronouslyCanceledTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            CancellationToken token = new CancellationToken(true);
            Task<Task> task = new Task[] { tcs.Task }.WhenAny(token);

            Assert.True(task.IsCanceled);
            Assert.Equal(token, GetCancellationTokenFromTask(task));
        }

        [Fact]
        public async Task WhenAny_TaskCompletes_CompletesTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<Task> task = new Task[] { tcs.Task }.WhenAny(cts.Token);
            Assert.False(task.IsCompleted);

            tcs.SetResult(null);

            Task result = await task;
            Assert.Same(tcs.Task, result);
        }

        [Fact]
        public async Task WhenAny_TokenCanceled_CancelsTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<Task> task = new Task[] { tcs.Task }.WhenAny(cts.Token);
            Assert.False(task.IsCompleted);

            cts.Cancel();

            await AsyncAssert.ThrowsAsync<OperationCanceledException>(task).ConfigureAwait(false);
            Assert.Equal(cts.Token, GetCancellationTokenFromTask(task));
        }

        [Fact]
        public async Task WhenAnyTResultWithoutToken_TaskCompletes_CompletesTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            Task<Task<object>> task = new[] { tcs.Task }.WhenAny();
            Assert.False(task.IsCompleted);

            tcs.SetResult(null);

            Task<object> result = await task;
            Assert.Same(tcs.Task, result);
        }

        [Fact]
        public async Task WhenAnyWithoutToken_TaskCompletes_CompletesTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            Task<Task> task = new Task[] { tcs.Task }.WhenAny();
            Assert.False(task.IsCompleted);

            tcs.SetResult(null);

            Task result = await task;
            Assert.Same(tcs.Task, result);
        }

        [Fact]
        public async Task WhenAllTResult_TaskCompletes_CompletesTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            Task<object[]> task = new[] { tcs.Task }.WhenAll();
            Assert.False(task.IsCompleted);

            object expectedResult = new object();
            tcs.SetResult(expectedResult);

            object[] result = await task;
            Assert.Equal(new[] { expectedResult }, result);
        }

        [Fact]
        public async Task WhenAll_TaskCompletes_CompletesTask()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            Task task = new Task[] { tcs.Task }.WhenAll();
            Assert.False(task.IsCompleted);

            object expectedResult = new object();
            tcs.SetResult(expectedResult);

            await task;
        }

        private static CancellationToken GetCancellationTokenFromTask(Task task)
        {
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                OperationCanceledException oce = ex.InnerException as OperationCanceledException;
                if (oce != null)
                {
                    return oce.CancellationToken;
                }
            }
            return CancellationToken.None;
        }
    }
}
