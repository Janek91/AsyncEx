using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Coordination.UnitTests
{
    public class AsyncManualResetEventUnitTests
    {
        [Fact]
        public async Task WaitAsync_Unset_IsNotCompleted()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();

            Task task = mre.WaitAsync();

            await AsyncAssert.NeverCompletesAsync(task).ConfigureAwait(false);
        }

        [Fact]
        public async Task Wait_Unset_IsNotCompleted()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();

            Task task = Task.Run(() => mre.Wait());

            await AsyncAssert.NeverCompletesAsync(task).ConfigureAwait(false);
        }

        [Fact]
        public void WaitAsync_AfterSet_IsCompleted()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();

            mre.Set();
            Task task = mre.WaitAsync();

            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void Wait_AfterSet_IsCompleted()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();

            mre.Set();
            mre.Wait();
        }

        [Fact]
        public void WaitAsync_Set_IsCompleted()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent(true);

            Task task = mre.WaitAsync();

            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void Wait_Set_IsCompleted()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent(true);

            mre.Wait();
        }

        [Fact]
        public void MultipleWaitAsync_AfterSet_IsCompleted()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();

            mre.Set();
            Task task1 = mre.WaitAsync();
            Task task2 = mre.WaitAsync();

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
        }

        [Fact]
        public void MultipleWait_AfterSet_IsCompleted()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();

            mre.Set();
            mre.Wait();
            mre.Wait();
        }

        [Fact]
        public void MultipleWaitAsync_Set_IsCompleted()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent(true);

            Task task1 = mre.WaitAsync();
            Task task2 = mre.WaitAsync();

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
        }

        [Fact]
        public void MultipleWait_Set_IsCompleted()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent(true);

            mre.Wait();
            mre.Wait();
        }

        [Fact]
        public async Task WaitAsync_AfterReset_IsNotCompleted()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();

            mre.Set();
            mre.Reset();
            Task task = mre.WaitAsync();

            await AsyncAssert.NeverCompletesAsync(task).ConfigureAwait(false);
        }

        [Fact]
        public async Task Wait_AfterReset_IsNotCompleted()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();

            mre.Set();
            mre.Reset();
            Task task = Task.Run(() => mre.Wait());

            await AsyncAssert.NeverCompletesAsync(task).ConfigureAwait(false);
        }

        [Fact]
        public void Id_IsNotZero()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();
            Assert.NotEqual(0, mre.Id);
        }
    }
}
