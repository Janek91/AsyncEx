using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Context.UnitTests
{
    public class AsyncContextUnitTests
    {
        [Fact]
        public void AsyncContext_StaysOnSameThread()
        {
            int testThread = Thread.CurrentThread.ManagedThreadId;
            int contextThread = AsyncContext.Run(() => Thread.CurrentThread.ManagedThreadId);
            Assert.Equal(testThread, contextThread);
        }

        [Fact]
        public void Run_AsyncVoid_BlocksUntilCompletion()
        {
            bool resumed = false;
            AsyncContext.Run((Action)(async () =>
            {
                await Task.Yield();
                resumed = true;
            }));
            Assert.True(resumed);
        }

        [Fact]
        public void Run_FuncThatCallsAsyncVoid_BlocksUntilCompletion()
        {
            bool resumed = false;
            int result = AsyncContext.Run((Func<int>)(() =>
            {
                Action asyncVoid = async () =>
                {
                    await Task.Yield();
                    resumed = true;
                };
                asyncVoid();
                return 13;
            }));
            Assert.True(resumed);
            Assert.Equal(13, result);
        }

        [Fact]
        public void Run_AsyncTask_BlocksUntilCompletion()
        {
            bool resumed = false;
            AsyncContext.Run(async () =>
            {
                await Task.Yield();
                resumed = true;
            });
            Assert.True(resumed);
        }

        [Fact]
        public void Run_AsyncTaskWithResult_BlocksUntilCompletion()
        {
            bool resumed = false;
            int result = AsyncContext.Run(async () =>
            {
                await Task.Yield();
                resumed = true;
                return 17;
            });
            Assert.True(resumed);
            Assert.Equal(17, result);
        }

        [Fact]
        public void Current_WithoutAsyncContext_IsNull()
        {
            Assert.Null(AsyncContext.Current);
        }

        [Fact]
        public void Current_FromAsyncContext_IsAsyncContext()
        {
            AsyncContext observedContext = null;
            AsyncContext context = new AsyncContext();
            context.Factory.Run(() => observedContext = AsyncContext.Current);

            context.Execute();

            Assert.Same(context, observedContext);
        }

        [Fact]
        public void SynchronizationContextCurrent_FromAsyncContext_IsAsyncContextSynchronizationContext()
        {
            SynchronizationContext observedContext = null;
            AsyncContext context = new AsyncContext();
            context.Factory.Run(() => observedContext = SynchronizationContext.Current);

            context.Execute();

            Assert.Same(context.SynchronizationContext, observedContext);
        }

        [Fact]
        public void TaskSchedulerCurrent_FromAsyncContext_IsThreadPoolTaskScheduler()
        {
            TaskScheduler observedScheduler = null;
            AsyncContext context = new AsyncContext();
            context.Factory.Run(() => observedScheduler = TaskScheduler.Current);

            context.Execute();

            Assert.Same(TaskScheduler.Default, observedScheduler);
        }

        [Fact]
        public void TaskScheduler_MaximumConcurrency_IsOne()
        {
            AsyncContext context = new AsyncContext();
            Assert.Equal(1, context.Scheduler.MaximumConcurrencyLevel);
        }

        [Fact]
        public void Run_PropagatesException()
        {
            Action test = () => AsyncContext.Run(() => throw new NotImplementedException());
            AsyncAssert.Throws<NotImplementedException>(test, allowDerivedTypes: false);
        }

        [Fact]
        public void Run_Async_PropagatesException()
        {
            Action test = () => AsyncContext.Run(async () => { await Task.Yield(); throw new NotImplementedException(); });
            AsyncAssert.Throws<NotImplementedException>(test, allowDerivedTypes: false);
        }

        [Fact]
        public void SynchronizationContextPost_PropagatesException()
        {
            Action test = () => AsyncContext.Run(async () =>
            {
                SynchronizationContext.Current.Post(_ => throw new NotImplementedException(), null);
                await Task.Yield();
            });
            AsyncAssert.Throws<NotImplementedException>(test, allowDerivedTypes: false);
        }

        [Fact]
        public async Task SynchronizationContext_Send_ExecutesSynchronously()
        {
            using (AsyncContextThread thread = new AsyncContextThread())
            {
                SynchronizationContext synchronizationContext = await thread.Factory.Run(() => SynchronizationContext.Current).ConfigureAwait(false);
                int value = 0;
                synchronizationContext.Send(_ => value = 13, null);
                Assert.Equal(13, value);
            }
        }

        [Fact]
        public async Task SynchronizationContext_Send_ExecutesInlineIfNecessary()
        {
            using (AsyncContextThread thread = new AsyncContextThread())
            {
                int value = 0;
                await thread.Factory.Run(() =>
                {
                    SynchronizationContext.Current.Send(_ => value = 13, null);
                    Assert.Equal(13, value);
                }).ConfigureAwait(false);
                Assert.Equal(13, value);
            }
        }

        [Fact]
        public void Task_AfterExecute_NeverRuns()
        {
            int value = 0;
            AsyncContext context = new AsyncContext();
            context.Factory.Run(() => value = 1);
            context.Execute();

            Task task = context.Factory.Run(() => value = 2);

            task.ContinueWith(_ => throw new Exception("Should not run"), TaskScheduler.Default);
            Assert.Equal(1, value);
        }

        [Fact]
        public void SynchronizationContext_IsEqualToCopyOfItself()
        {
            SynchronizationContext synchronizationContext1 = AsyncContext.Run(() => SynchronizationContext.Current);
            SynchronizationContext synchronizationContext2 = synchronizationContext1.CreateCopy();
            Assert.Equal(synchronizationContext1.GetHashCode(), synchronizationContext2.GetHashCode());
            Assert.True(synchronizationContext1.Equals(synchronizationContext2));
            Assert.False(synchronizationContext1.Equals(new SynchronizationContext()));
        }

        [Fact]
        public void Id_IsEqualToTaskSchedulerId()
        {
            AsyncContext context = new AsyncContext();
            Assert.Equal(context.Scheduler.Id, context.Id);
        }
    }
}
