using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Xunit;

namespace AsyncEx.Context.UnitTests
{
    public class AsyncContextThreadUnitTests
    {
        [Fact]
        public async Task AsyncContextThread_IsAnIndependentThread()
        {
            int testThread = Thread.CurrentThread.ManagedThreadId;
            AsyncContextThread thread = new AsyncContextThread();
            int contextThread = await thread.Factory.Run(() => Thread.CurrentThread.ManagedThreadId);
            Assert.NotEqual(testThread, contextThread);
            await thread.JoinAsync();
        }

        [Fact]
        public async Task AsyncDelegate_ResumesOnSameThread()
        {
            AsyncContextThread thread = new AsyncContextThread();
            int contextThread = -1, resumeThread = -1;
            await thread.Factory.Run(async () =>
            {
                contextThread = Thread.CurrentThread.ManagedThreadId;
                await Task.Yield();
                resumeThread = Thread.CurrentThread.ManagedThreadId;
            });
            Assert.Equal(contextThread, resumeThread);
            await thread.JoinAsync();
        }

        [Fact]
        public async Task Join_StopsTask()
        {
            AsyncContextThread context = new AsyncContextThread();
            Thread thread = await context.Factory.Run(() => Thread.CurrentThread);
            await context.JoinAsync();
        }

        [Fact]
        public async Task Context_IsCorrectAsyncContext()
        {
            using (AsyncContextThread thread = new AsyncContextThread())
            {
                AsyncContext observedContext = await thread.Factory.Run(() => AsyncContext.Current);
                Assert.Same(observedContext, thread.Context);
            }
        }
    }
}
