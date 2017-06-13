using System.Threading.Tasks;
using Nito.AsyncEx;
using Xunit;

namespace AsyncEx.Tasks.UnitTests
{
    public class TaskFactoryExtensionsUnitTests
    {
        [Fact]
        public async Task RunAction_WithFactoryScheduler_UsesFactoryScheduler()
        {
            TaskScheduler scheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
            TaskFactory factory = new TaskFactory(scheduler);
            TaskScheduler result = null;

            Task task = factory.Run(() =>
            {
                result = TaskScheduler.Current;
            });
            await task;

            Assert.Same(scheduler, result);
            Assert.True((task.CreationOptions & TaskCreationOptions.DenyChildAttach) == TaskCreationOptions.DenyChildAttach);
        }

        [Fact]
        public async Task RunAction_WithCurrentScheduler_UsesDefaultScheduler()
        {
            TaskScheduler scheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
            TaskFactory testFactory = new TaskFactory(scheduler);
            Task task = null;
            TaskScheduler result = null;

            await testFactory.StartNew(async () =>
            {
                Assert.Same(scheduler, TaskScheduler.Current);
                Assert.Null(Task.Factory.Scheduler);
                task = Task.Factory.Run(() =>
                {
                    result = TaskScheduler.Current;
                });
                await task;
            }).Unwrap();

            Assert.Same(TaskScheduler.Default, result);
            Assert.True((task.CreationOptions & TaskCreationOptions.DenyChildAttach) == TaskCreationOptions.DenyChildAttach);
        }

        [Fact]
        public async Task RunFunc_WithFactoryScheduler_UsesFactoryScheduler()
        {
            TaskScheduler scheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
            TaskFactory factory = new TaskFactory(scheduler);

            Task<TaskScheduler> task = factory.Run(() => TaskScheduler.Current);
            TaskScheduler result = await task;

            Assert.Same(scheduler, result);
            Assert.True((task.CreationOptions & TaskCreationOptions.DenyChildAttach) == TaskCreationOptions.DenyChildAttach);
        }

        [Fact]
        public async Task RunFunc_WithCurrentScheduler_UsesDefaultScheduler()
        {
            TaskScheduler scheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
            TaskFactory testFactory = new TaskFactory(scheduler);
            Task<TaskScheduler> task = null;
            TaskScheduler result = null;

            await testFactory.StartNew(async () =>
            {
                Assert.Same(scheduler, TaskScheduler.Current);
                Assert.Null(Task.Factory.Scheduler);
                task = Task.Factory.Run(() => TaskScheduler.Current);
                result = await task;
            }).Unwrap();

            Assert.Same(TaskScheduler.Default, result);
            Assert.True((task.CreationOptions & TaskCreationOptions.DenyChildAttach) == TaskCreationOptions.DenyChildAttach);
        }

        [Fact]
        public async Task RunAsyncAction_WithFactoryScheduler_UsesFactoryScheduler()
        {
            TaskScheduler scheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
            TaskFactory factory = new TaskFactory(scheduler);
            TaskScheduler result = null;
            TaskScheduler resultAfterAwait = null;

            Task task = factory.Run(async () =>
            {
                result = TaskScheduler.Current;
                await Task.Yield();
                resultAfterAwait = TaskScheduler.Current;
            });
            await task;

            Assert.Same(scheduler, result);
            Assert.Same(scheduler, resultAfterAwait);
        }

        [Fact]
        public async Task RunAsyncAction_WithCurrentScheduler_UsesDefaultScheduler()
        {
            TaskScheduler scheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
            TaskFactory testFactory = new TaskFactory(scheduler);
            TaskScheduler result = null;
            TaskScheduler resultAfterAwait = null;

            await testFactory.StartNew(async () =>
            {
                Assert.Same(scheduler, TaskScheduler.Current);
                Assert.Null(Task.Factory.Scheduler);
                await Task.Factory.Run(async () =>
                {
                    result = TaskScheduler.Current;
                    await Task.Yield();
                    resultAfterAwait = TaskScheduler.Current;
                });
            }).Unwrap();

            Assert.Same(TaskScheduler.Default, result);
            Assert.Same(TaskScheduler.Default, resultAfterAwait);
        }

        [Fact]
        public async Task RunAsyncFunc_WithFactoryScheduler_UsesFactoryScheduler()
        {
            TaskScheduler scheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
            TaskFactory factory = new TaskFactory(scheduler);
            TaskScheduler result = null;

            TaskScheduler resultAfterAwait = await factory.Run(async () =>
            {
                result = TaskScheduler.Current;
                await Task.Yield();
                return TaskScheduler.Current;
            });

            Assert.Same(scheduler, result);
            Assert.Same(scheduler, resultAfterAwait);
        }

        [Fact]
        public async Task RunAsyncFunc_WithCurrentScheduler_UsesDefaultScheduler()
        {
            TaskScheduler scheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
            TaskFactory testFactory = new TaskFactory(scheduler);
            TaskScheduler result = null;
            TaskScheduler resultAfterAwait = null;

            await testFactory.StartNew(async () =>
            {
                Assert.Same(scheduler, TaskScheduler.Current);
                Assert.Null(Task.Factory.Scheduler);
                resultAfterAwait = await Task.Factory.Run(async () =>
                {
                    result = TaskScheduler.Current;
                    await Task.Yield();
                    return TaskScheduler.Current;
                });
            }).Unwrap();

            Assert.Same(TaskScheduler.Default, result);
            Assert.Same(TaskScheduler.Default, resultAfterAwait);
        }
    }
}
