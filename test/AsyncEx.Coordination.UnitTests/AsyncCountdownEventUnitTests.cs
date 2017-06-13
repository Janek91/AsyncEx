using System;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Coordination.UnitTests
{
    public class AsyncCountdownEventUnitTests
    {
        [Fact]
        public async Task WaitAsync_Unset_IsNotCompleted()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(1);
            Task task = ce.WaitAsync();

            Assert.Equal(1, ce.CurrentCount);
            Assert.False(task.IsCompleted);

            ce.Signal();
            await task;
        }

        [Fact]
        public void WaitAsync_Set_IsCompleted()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(0);
            Task task = ce.WaitAsync();

            Assert.Equal(0, ce.CurrentCount);
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task AddCount_IncrementsCount()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(1);
            Task task = ce.WaitAsync();
            Assert.Equal(1, ce.CurrentCount);
            Assert.False(task.IsCompleted);

            ce.AddCount();

            Assert.Equal(2, ce.CurrentCount);
            Assert.False(task.IsCompleted);

            ce.Signal(2);
            await task;
        }

        [Fact]
        public async Task Signal_Nonzero_IsNotCompleted()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(2);
            Task task = ce.WaitAsync();
            Assert.False(task.IsCompleted);

            ce.Signal();

            Assert.Equal(1, ce.CurrentCount);
            Assert.False(task.IsCompleted);

            ce.Signal();
            await task;
        }

        [Fact]
        public void Signal_Zero_SynchronouslyCompletesWaitTask()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(1);
            Task task = ce.WaitAsync();
            Assert.False(task.IsCompleted);

            ce.Signal();

            Assert.Equal(0, ce.CurrentCount);
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task Signal_AfterSet_CountsNegativeAndResetsTask()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(0);
            Task originalTask = ce.WaitAsync();

            ce.Signal();

            Task newTask = ce.WaitAsync();
            Assert.Equal(-1, ce.CurrentCount);
            Assert.NotSame(originalTask, newTask);

            ce.AddCount();
            await newTask;
        }

        [Fact]
        public async Task AddCount_AfterSet_CountsPositiveAndResetsTask()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(0);
            Task originalTask = ce.WaitAsync();

            ce.AddCount();
            Task newTask = ce.WaitAsync();

            Assert.Equal(1, ce.CurrentCount);
            Assert.NotSame(originalTask, newTask);

            ce.Signal();
            await newTask;
        }

        [Fact]
        public async Task Signal_PastZero_PulsesTask()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(1);
            Task originalTask = ce.WaitAsync();

            ce.Signal(2);
            await originalTask;
            Task newTask = ce.WaitAsync();

            Assert.Equal(-1, ce.CurrentCount);
            Assert.NotSame(originalTask, newTask);

            ce.AddCount();
            await newTask;
        }

        [Fact]
        public async Task AddCount_PastZero_PulsesTask()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(-1);
            Task originalTask = ce.WaitAsync();

            ce.AddCount(2);
            await originalTask;
            Task newTask = ce.WaitAsync();

            Assert.Equal(1, ce.CurrentCount);
            Assert.NotSame(originalTask, newTask);

            ce.Signal();
            await newTask;
        }

        [Fact]
        public void AddCount_Overflow_ThrowsException()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(long.MaxValue);
            AsyncAssert.Throws<OverflowException>(() => ce.AddCount());
        }

        [Fact]
        public void Signal_Underflow_ThrowsException()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(long.MinValue);
            AsyncAssert.Throws<OverflowException>(() => ce.Signal());
        }

        [Fact]
        public void Id_IsNotZero()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(0);
            Assert.NotEqual(0, ce.Id);
        }
    }
}
