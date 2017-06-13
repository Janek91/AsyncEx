using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Tasks.UnitTests
{
    public class SynchronousTaskExtensionsUnitTests
    {
        [Fact]
        public void WaitAndUnwrapException_Completed_DoesNotBlock()
        {
            TaskConstants.Completed.WaitAndUnwrapException();
        }

        [Fact]
        public void WaitAndUnwrapException_Faulted_UnwrapsException()
        {
            Task task = Task.Run(() => { throw new NotImplementedException(); });
            AsyncAssert.Throws<NotImplementedException>(() => task.WaitAndUnwrapException());
        }

        [Fact]
        public void WaitAndUnwrapExceptionWithCT_Completed_DoesNotBlock()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            TaskConstants.Completed.WaitAndUnwrapException(cts.Token);
        }

        [Fact]
        public void WaitAndUnwrapExceptionWithCT_Faulted_UnwrapsException()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Task task = Task.Run(() => { throw new NotImplementedException(); });
            AsyncAssert.Throws<NotImplementedException>(() => task.WaitAndUnwrapException(cts.Token));
        }

        [Fact]
        public void WaitAndUnwrapExceptionWithCT_CancellationTokenCancelled_Cancels()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            Task task = tcs.Task;
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            AsyncAssert.Throws<OperationCanceledException>(() => task.WaitAndUnwrapException(cts.Token));
        }

        [Fact]
        public void WaitAndUnwrapExceptionResult_Completed_DoesNotBlock()
        {
            TaskConstants.Int32Zero.WaitAndUnwrapException();
        }

        [Fact]
        public void WaitAndUnwrapExceptionResult_Faulted_UnwrapsException()
        {
            Task<int> task = Task.Run((Func<int>)(() => { throw new NotImplementedException(); }));
            AsyncAssert.Throws<NotImplementedException>(() => task.WaitAndUnwrapException(), allowDerivedTypes: false);
        }

        [Fact]
        public void WaitAndUnwrapExceptionResultWithCT_Completed_DoesNotBlock()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            TaskConstants.Int32Zero.WaitAndUnwrapException(cts.Token);
        }

        [Fact]
        public void WaitAndUnwrapExceptionResultWithCT_Faulted_UnwrapsException()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<int> task = Task.Run((Func<int>)(() => { throw new NotImplementedException(); }));
            AsyncAssert.Throws<NotImplementedException>(() => task.WaitAndUnwrapException(cts.Token), allowDerivedTypes: false);
        }

        [Fact]
        public void WaitAndUnwrapExceptionResultWithCT_CancellationTokenCancelled_Cancels()
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            AsyncAssert.Throws<OperationCanceledException>(() => tcs.Task.WaitAndUnwrapException(cts.Token));
        }

        [Fact]
        public void WaitWithoutException_Completed_DoesNotBlock()
        {
            TaskConstants.Completed.WaitWithoutException();
        }

        [Fact]
        public void WaitWithoutException_Canceled_DoesNotBlockOrThrow()
        {
            TaskConstants.Canceled.WaitWithoutException();
        }

        [Fact]
        public void WaitWithoutException_Faulted_DoesNotBlockOrThrow()
        {
            Task task = Task.Run(() => { throw new NotImplementedException(); });
            task.WaitWithoutException();
        }

        [Fact]
        public void WaitWithoutExceptionResult_Completed_DoesNotBlock()
        {
            TaskConstants.Int32Zero.WaitWithoutException();
        }

        [Fact]
        public void WaitWithoutExceptionResult_Canceled_DoesNotBlockOrThrow()
        {
            TaskConstants<int>.Canceled.WaitWithoutException();
        }

        [Fact]
        public void WaitWithoutExceptionResult_Faulted_DoesNotBlockOrThrow()
        {
            Task<int> task = Task.Run((Func<int>)(() => { throw new NotImplementedException(); }));
            task.WaitWithoutException();
        }

        [Fact]
        public void WaitWithoutExceptionWithCancellationToken_Completed_DoesNotBlock()
        {
            TaskConstants.Completed.WaitWithoutException(new CancellationToken());
        }

        [Fact]
        public void WaitWithoutExceptionWithCancellationToken_Canceled_DoesNotBlockOrThrow()
        {
            TaskConstants.Canceled.WaitWithoutException(new CancellationToken());
        }

        [Fact]
        public void WaitWithoutExceptionWithCancellationToken_Faulted_DoesNotBlockOrThrow()
        {
            Task task = Task.Run(() => { throw new NotImplementedException(); });
            task.WaitWithoutException(new CancellationToken());
        }

        [Fact]
        public void WaitWithoutExceptionResultWithCancellationToken_Completed_DoesNotBlock()
        {
            TaskConstants.Int32Zero.WaitWithoutException(new CancellationToken());
        }

        [Fact]
        public void WaitWithoutExceptionResultWithCancellationToken_Canceled_DoesNotBlockOrThrow()
        {
            TaskConstants<int>.Canceled.WaitWithoutException(new CancellationToken());
        }

        [Fact]
        public void WaitWithoutExceptionResultWithCancellationToken_Faulted_DoesNotBlockOrThrow()
        {
            Task<int> task = Task.Run((Func<int>)(() => { throw new NotImplementedException(); }));
            task.WaitWithoutException(new CancellationToken());
        }

        [Fact]
        public void WaitWithoutExceptionWithCancellationToken_CanceledToken_DoesNotBlockButThrowsException()
        {
            Task task = new TaskCompletionSource<object>().Task;
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            AsyncAssert.Throws<OperationCanceledException>(() => task.WaitWithoutException(cts.Token));
        }

        [Fact]
        public async Task WaitWithoutExceptionWithCancellationToken_TokenCanceled_ThrowsException()
        {
            Task sourceTask = new TaskCompletionSource<object>().Task;
            CancellationTokenSource cts = new CancellationTokenSource();
            Task task = Task.Run(() => sourceTask.WaitWithoutException(cts.Token));
            bool result = task.Wait(500);
            Assert.False(result);
            cts.Cancel();
            await AsyncAssert.ThrowsAsync<OperationCanceledException>(() => task);
        }
    }
}
