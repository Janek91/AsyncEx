using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

#pragma warning disable CS0162

namespace AsyncEx.Coordination.UnitTests
{
    public class AsyncLazyUnitTests
    {
        [Fact]
        public void AsyncLazy_NeverAwaited_DoesNotCallFunc()
        {
            Func<Task<int>> func = () =>
            {
                throw new Exception();
                return Task.FromResult(13);
            };

            AsyncLazy<int> lazy = new AsyncLazy<int>(func);
        }

        [Fact]
        public async Task AsyncLazy_WithCallDirectFlag_CallsFuncDirectly()
        {
            int testThread = Thread.CurrentThread.ManagedThreadId;
            int funcThread = testThread + 1;
            Func<Task<int>> func = () =>
            {
                funcThread = Thread.CurrentThread.ManagedThreadId;
                return Task.FromResult(13);
            };
            AsyncLazy<int> lazy = new AsyncLazy<int>(func, AsyncLazyFlags.ExecuteOnCallingThread);

            await lazy;

            Assert.Equal(testThread, funcThread);
        }

        [Fact]
        public async Task AsyncLazy_ByDefault_CallsFuncOnThreadPool()
        {
            int testThread = Thread.CurrentThread.ManagedThreadId;
            int funcThread = testThread;
            Func<Task<int>> func = () =>
            {
                funcThread = Thread.CurrentThread.ManagedThreadId;
                return Task.FromResult(13);
            };
            AsyncLazy<int> lazy = new AsyncLazy<int>(func);

            await lazy;

            Assert.NotEqual(testThread, funcThread);
        }

        [Fact]
        public async Task AsyncLazy_Start_CallsFunc()
        {
            TaskCompletionSource<object> tcs = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            Func<Task<int>> func = () =>
            {
                tcs.SetResult(null);
                return Task.FromResult(13);
            };
            AsyncLazy<int> lazy = new AsyncLazy<int>(func);

            lazy.Start();
            await tcs.Task;
        }

        [Fact]
        public async Task AsyncLazy_Await_ReturnsFuncValue()
        {
            Func<Task<int>> func = async () =>
            {
                await Task.Yield();
                return 13;
            };
            AsyncLazy<int> lazy = new AsyncLazy<int>(func);

            int result = await lazy;
            Assert.Equal(13, result);
        }

        [Fact]
        public async Task AsyncLazy_MultipleAwaiters_OnlyInvokeFuncOnce()
        {
            int invokeCount = 0;
            TaskCompletionSource<object> tcs = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            Func<Task<int>> func = async () =>
            {
                Interlocked.Increment(ref invokeCount);
                await tcs.Task;
                return 13;
            };
            AsyncLazy<int> lazy = new AsyncLazy<int>(func);

            Task<int> task1 = Task.Run(async () => await lazy);
            Task<int> task2 = Task.Run(async () => await lazy);

            Assert.False(task1.IsCompleted);
            Assert.False(task2.IsCompleted);
            tcs.SetResult(null);
            int[] results = await Task.WhenAll(task1, task2).ConfigureAwait(false);
            Assert.True(results.SequenceEqual(new[] { 13, 13 }));
            Assert.Equal(1, invokeCount);
        }

        [Fact]
        public async Task AsyncLazy_FailureCachedByDefault()
        {
            int invokeCount = 0;
            Func<Task<int>> func = async () =>
            {
                Interlocked.Increment(ref invokeCount);
                await Task.Yield();
                if (invokeCount == 1)
                {
                    throw new InvalidOperationException("Not today, punk.");
                }
                return 13;
            };
            AsyncLazy<int> lazy = new AsyncLazy<int>(func);
            await AsyncAssert.ThrowsAsync<InvalidOperationException>(lazy.Task).ConfigureAwait(false);

            await AsyncAssert.ThrowsAsync<InvalidOperationException>(lazy.Task).ConfigureAwait(false);
            Assert.Equal(1, invokeCount);
        }

        [Fact]
        public async Task AsyncLazy_WithRetryOnFailure_DoesNotCacheFailure()
        {
            int invokeCount = 0;
            Func<Task<int>> func = async () =>
            {
                Interlocked.Increment(ref invokeCount);
                await Task.Yield();
                if (invokeCount == 1)
                {
                    throw new InvalidOperationException("Not today, punk.");
                }
                return 13;
            };
            AsyncLazy<int> lazy = new AsyncLazy<int>(func, AsyncLazyFlags.RetryOnFailure);
            await AsyncAssert.ThrowsAsync<InvalidOperationException>(lazy.Task).ConfigureAwait(false);

            Assert.Equal(13, await lazy);
            Assert.Equal(2, invokeCount);
        }

        [Fact]
        public async Task AsyncLazy_WithRetryOnFailure_DoesNotRetryOnSuccess()
        {
            int invokeCount = 0;
            Func<Task<int>> func = async () =>
            {
                Interlocked.Increment(ref invokeCount);
                await Task.Yield();
                if (invokeCount == 1)
                {
                    throw new InvalidOperationException("Not today, punk.");
                }
                return 13;
            };
            AsyncLazy<int> lazy = new AsyncLazy<int>(func, AsyncLazyFlags.RetryOnFailure);
            await AsyncAssert.ThrowsAsync<InvalidOperationException>(lazy.Task).ConfigureAwait(false);

            await lazy;
            await lazy;

            Assert.Equal(13, await lazy);
            Assert.Equal(2, invokeCount);
        }

        [Fact]
        public void Id_IsNotZero()
        {
            AsyncLazy<object> lazy = new AsyncLazy<object>(() => Task.FromResult<object>(null));
            Assert.NotEqual(0, lazy.Id);
        }
    }
}
