using System.Threading;
using Nito.AsyncEx;
using Xunit;

namespace AsyncEx.Tasks.UnitTests
{
    public class CancellationTokenTaskSourceUnitTests
    {
        [Fact]
        public void Constructor_AlreadyCanceledToken_TaskReturnsSynchronouslyCanceledTask()
        {
            CancellationToken token = new CancellationToken(true);
            using (CancellationTokenTaskSource<object> source = new CancellationTokenTaskSource<object>(token))
            {
                Assert.True(source.Task.IsCanceled);
            }
        }
    }
}
