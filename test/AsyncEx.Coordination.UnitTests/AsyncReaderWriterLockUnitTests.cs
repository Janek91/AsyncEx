using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Coordination.UnitTests
{
    public class AsyncReaderWriterLockUnitTests
    {
        [Fact]
        public async Task Unlocked_PermitsWriterLock()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            await rwl.WriterLockAsync();
        }

        [Fact]
        public async Task Unlocked_PermitsMultipleReaderLocks()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            await rwl.ReaderLockAsync();
            await rwl.ReaderLockAsync();
        }

        [Fact]
        public async Task WriteLocked_PreventsAnotherWriterLock()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            await rwl.WriterLockAsync();
            Task<IDisposable> task = rwl.WriterLockAsync().AsTask();
            await AsyncAssert.NeverCompletesAsync(task);
        }

        [Fact]
        public async Task WriteLocked_PreventsReaderLock()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            await rwl.WriterLockAsync();
            Task<IDisposable> task = rwl.ReaderLockAsync().AsTask();
            await AsyncAssert.NeverCompletesAsync(task);
        }

        [Fact]
        public async Task WriteLocked_Unlocked_PermitsAnotherWriterLock()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            TaskCompletionSource<object> firstWriteLockTaken = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            TaskCompletionSource<object> releaseFirstWriteLock = TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();
            Task task = Task.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    firstWriteLockTaken.SetResult(null);
                    await releaseFirstWriteLock.Task;
                }
            });
            await firstWriteLockTaken.Task;
            Task<IDisposable> lockTask = rwl.WriterLockAsync().AsTask();
            Assert.False(lockTask.IsCompleted);
            releaseFirstWriteLock.SetResult(null);
            await lockTask;
        }

        [Fact]
        public async Task ReadLocked_PreventsWriterLock()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            await rwl.ReaderLockAsync();
            Task<IDisposable> task = rwl.WriterLockAsync().AsTask();
            await AsyncAssert.NeverCompletesAsync(task);
        }

        [Fact]
        public void Id_IsNotZero()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            Assert.NotEqual(0, rwl.Id);
        }

        [Fact]
        public void WriterLock_PreCancelled_LockAvailable_SynchronouslyTakesLock()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            CancellationToken token = new CancellationToken(true);

            Task<IDisposable> task = rwl.WriterLockAsync(token).AsTask();

            Assert.True(task.IsCompleted);
            Assert.False(task.IsCanceled);
            Assert.False(task.IsFaulted);
        }

        [Fact]
        public void WriterLock_PreCancelled_LockNotAvailable_SynchronouslyCancels()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            CancellationToken token = new CancellationToken(true);
            rwl.WriterLockAsync();

            Task<IDisposable> task = rwl.WriterLockAsync(token).AsTask();

            Assert.True(task.IsCompleted);
            Assert.True(task.IsCanceled);
            Assert.False(task.IsFaulted);
        }

        [Fact]
        public void ReaderLock_PreCancelled_LockAvailable_SynchronouslyTakesLock()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            CancellationToken token = new CancellationToken(true);

            Task<IDisposable> task = rwl.ReaderLockAsync(token).AsTask();

            Assert.True(task.IsCompleted);
            Assert.False(task.IsCanceled);
            Assert.False(task.IsFaulted);
        }

        [Fact]
        public void ReaderLock_PreCancelled_LockNotAvailable_SynchronouslyCancels()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            CancellationToken token = new CancellationToken(true);
            rwl.WriterLockAsync();

            Task<IDisposable> task = rwl.ReaderLockAsync(token).AsTask();

            Assert.True(task.IsCompleted);
            Assert.True(task.IsCanceled);
            Assert.False(task.IsFaulted);
        }

        [Fact]
        public async Task WriteLocked_WriterLockCancelled_DoesNotTakeLockWhenUnlocked()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            using (await rwl.WriterLockAsync())
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                Task<IDisposable> task = rwl.WriterLockAsync(cts.Token).AsTask();
                cts.Cancel();
                await AsyncAssert.ThrowsAsync<OperationCanceledException>(task);
            }

            await rwl.WriterLockAsync();
        }

        [Fact]
        public async Task WriteLocked_ReaderLockCancelled_DoesNotTakeLockWhenUnlocked()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            using (await rwl.WriterLockAsync())
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                Task<IDisposable> task = rwl.ReaderLockAsync(cts.Token).AsTask();
                cts.Cancel();
                await AsyncAssert.ThrowsAsync<OperationCanceledException>(task);
            }

            await rwl.ReaderLockAsync();
        }

        [Fact]
        public async Task LockReleased_WriteTakesPriorityOverRead()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            Task writeLock, readLock;
            using (await rwl.WriterLockAsync())
            {
                readLock = rwl.ReaderLockAsync().AsTask();
                writeLock = rwl.WriterLockAsync().AsTask();
            }

            await writeLock;
            await AsyncAssert.NeverCompletesAsync(readLock);
        }

        [Fact]
        public async Task ReaderLocked_ReaderReleased_ReaderAndWriterWaiting_DoesNotReleaseReaderOrWriter()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            Task readLock, writeLock;
            await rwl.ReaderLockAsync();
            using (await rwl.ReaderLockAsync())
            {
                writeLock = rwl.WriterLockAsync().AsTask();
                readLock = rwl.ReaderLockAsync().AsTask();
            }

            await Task.WhenAll(AsyncAssert.NeverCompletesAsync(writeLock),
                AsyncAssert.NeverCompletesAsync(readLock));
        }

        [Fact]
        public async Task LoadTest()
        {
            AsyncReaderWriterLock rwl = new AsyncReaderWriterLock();
            List<IDisposable> readKeys = new List<IDisposable>();
            for (int i = 0; i != 1000; ++i)
            {
                readKeys.Add(rwl.ReaderLock());
            }
            Task writeTask = Task.Run(() => { rwl.WriterLock().Dispose(); });
            List<Task> readTasks = new List<Task>();
            for (int i = 0; i != 100; ++i)
            {
                readTasks.Add(Task.Run(() => rwl.ReaderLock().Dispose()));
            }
            await Task.Delay(1000);
            foreach (IDisposable readKey in readKeys)
            {
                readKey.Dispose();
            }
            await writeTask;
            foreach (Task readTask in readTasks)
            {
                await readTask;
            }
        }
    }
}
