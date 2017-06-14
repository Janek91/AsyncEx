using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx.Interop;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Interop.WaitHandles.UnitTests
{
    public class WaitHandleInteropUnitTests
    {
        [Fact]
        public void FromWaitHandle_SignaledHandle_SynchronouslyCompletes()
        {
            ManualResetEvent mre = new ManualResetEvent(true);
            Task task = WaitHandleAsyncFactory.FromWaitHandle(mre);
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void FromWaitHandle_SignaledHandleWithZeroTimeout_SynchronouslyCompletesWithTrueResult()
        {
            ManualResetEvent mre = new ManualResetEvent(true);
            Task<bool> task = WaitHandleAsyncFactory.FromWaitHandle(mre, TimeSpan.Zero);
            Assert.True(task.IsCompleted);
            Assert.True(task.Result);
        }

        [Fact]
        public void FromWaitHandle_UnsignaledHandleWithZeroTimeout_SynchronouslyCompletesWithFalseResult()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            Task<bool> task = WaitHandleAsyncFactory.FromWaitHandle(mre, TimeSpan.Zero);
            Assert.True(task.IsCompleted);
            Assert.False(task.Result);
        }

        [Fact]
        public void FromWaitHandle_SignaledHandleWithCanceledToken_SynchronouslyCompletes()
        {
            ManualResetEvent mre = new ManualResetEvent(true);
            Task task = WaitHandleAsyncFactory.FromWaitHandle(mre, new CancellationToken(true));
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void FromWaitHandle_UnsignaledHandleWithCanceledToken_SynchronouslyCancels()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            Task task = WaitHandleAsyncFactory.FromWaitHandle(mre, new CancellationToken(true));
            Assert.True(task.IsCompleted);
            Assert.True(task.IsCanceled);
        }

        [Fact]
        public void FromWaitHandle_SignaledHandleWithZeroTimeoutAndCanceledToken_SynchronouslyCompletesWithTrueResult()
        {
            ManualResetEvent mre = new ManualResetEvent(true);
            Task<bool> task = WaitHandleAsyncFactory.FromWaitHandle(mre, TimeSpan.Zero, new CancellationToken(true));
            Assert.True(task.IsCompleted);
            Assert.True(task.Result);
        }

        [Fact]
        public void FromWaitHandle_UnsignaledHandleWithZeroTimeoutAndCanceledToken_SynchronouslyCompletesWithFalseResult()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            Task<bool> task = WaitHandleAsyncFactory.FromWaitHandle(mre, TimeSpan.Zero, new CancellationToken(true));
            Assert.True(task.IsCompleted);
            Assert.False(task.Result);
        }

        [Fact]
        public async Task FromWaitHandle_HandleSignalled_Completes()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            Task task = WaitHandleAsyncFactory.FromWaitHandle(mre);
            Assert.False(task.IsCompleted);
            mre.Set();
            await task;
        }

        [Fact]
        public async Task FromWaitHandle_HandleSignalledBeforeTimeout_CompletesWithTrueResult()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            Task<bool> task = WaitHandleAsyncFactory.FromWaitHandle(mre, Timeout.InfiniteTimeSpan);
            Assert.False(task.IsCompleted);
            mre.Set();
            bool result = await task;
            Assert.True(result);
        }

        [Fact]
        public async Task FromWaitHandle_TimeoutBeforeHandleSignalled_CompletesWithFalseResult()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            Task<bool> task = WaitHandleAsyncFactory.FromWaitHandle(mre, TimeSpan.FromMilliseconds(10));
            bool result = await task;
            Assert.False(result);
        }

        [Fact]
        public async Task FromWaitHandle_HandleSignalledBeforeCanceled_CompletesSuccessfully()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            CancellationTokenSource cts = new CancellationTokenSource();
            Task task = WaitHandleAsyncFactory.FromWaitHandle(mre, cts.Token);
            Assert.False(task.IsCompleted);
            mre.Set();
            await task;
        }

        [Fact]
        public async Task FromWaitHandle_CanceledBeforeHandleSignalled_CompletesCanceled()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            CancellationTokenSource cts = new CancellationTokenSource();
            Task task = WaitHandleAsyncFactory.FromWaitHandle(mre, cts.Token);
            Assert.False(task.IsCompleted);
            cts.Cancel();
            await AsyncAssert.CancelsAsync(task).ConfigureAwait(false);
        }

        [Fact]
        public async Task FromWaitHandle_HandleSignalledBeforeTimeoutOrCanceled_CompletesWithTrueResult()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<bool> task = WaitHandleAsyncFactory.FromWaitHandle(mre, Timeout.InfiniteTimeSpan, cts.Token);
            Assert.False(task.IsCompleted);
            mre.Set();
            bool result = await task;
            Assert.True(result);
        }

        [Fact]
        public async Task FromWaitHandle_TimeoutBeforeHandleSignalledOrCanceled_CompletesWithFalseResult()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<bool> task = WaitHandleAsyncFactory.FromWaitHandle(mre, TimeSpan.FromMilliseconds(10), cts.Token);
            bool result = await task;
            Assert.False(result);
        }

        [Fact]
        public async Task FromWaitHandle_CanceledBeforeTimeoutOrHandleSignalled_CompletesCanceled()
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<bool> task = WaitHandleAsyncFactory.FromWaitHandle(mre, Timeout.InfiniteTimeSpan, cts.Token);
            Assert.False(task.IsCompleted);
            cts.Cancel();
            await AsyncAssert.CancelsAsync(task).ConfigureAwait(false);
        }
    }
}
