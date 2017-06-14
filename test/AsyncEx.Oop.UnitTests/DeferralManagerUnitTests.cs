using System;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Testing;
using Xunit;

namespace AsyncEx.Oop.UnitTests
{
    public class DeferralManagerUnitTests
    {
        [Fact]
        public void NoDeferrals_IsCompleted()
        {
            DeferralManager dm = new DeferralManager();
            Task task = dm.WaitForDeferralsAsync();
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task IncompleteDeferral_PreventsCompletion()
        {
            DeferralManager dm = new DeferralManager();
            IDisposable deferral = dm.DeferralSource.GetDeferral();
            await AsyncAssert.NeverCompletesAsync(dm.WaitForDeferralsAsync()).ConfigureAwait(false);
        }

        [Fact]
        public async Task DeferralCompleted_Completes()
        {
            DeferralManager dm = new DeferralManager();
            IDisposable deferral = dm.DeferralSource.GetDeferral();
            Task task = dm.WaitForDeferralsAsync();
            Assert.False(task.IsCompleted);
            deferral.Dispose();
            await task;
        }

        [Fact]
        public async Task MultipleDeferralsWithOneIncomplete_PreventsCompletion()
        {
            DeferralManager dm = new DeferralManager();
            IDisposable deferral1 = dm.DeferralSource.GetDeferral();
            IDisposable deferral2 = dm.DeferralSource.GetDeferral();
            Task task = dm.WaitForDeferralsAsync();
            deferral1.Dispose();
            await AsyncAssert.NeverCompletesAsync(task).ConfigureAwait(false);
        }

        [Fact]
        public async Task TwoDeferralsWithOneCompletedTwice_PreventsCompletion()
        {
            DeferralManager dm = new DeferralManager();
            IDisposable deferral1 = dm.DeferralSource.GetDeferral();
            IDisposable deferral2 = dm.DeferralSource.GetDeferral();
            Task task = dm.WaitForDeferralsAsync();
            deferral1.Dispose();
            deferral1.Dispose();
            await AsyncAssert.NeverCompletesAsync(task).ConfigureAwait(false);
        }

        [Fact]
        public async Task MultipleDeferralsWithAllCompleted_Completes()
        {
            DeferralManager dm = new DeferralManager();
            IDisposable deferral1 = dm.DeferralSource.GetDeferral();
            IDisposable deferral2 = dm.DeferralSource.GetDeferral();
            Task task = dm.WaitForDeferralsAsync();
            deferral1.Dispose();
            deferral2.Dispose();
            await task;
        }

        [Fact]
        public async Task CompletedDeferralFollowedByIncompleteDeferral_PreventsCompletion()
        {
            DeferralManager dm = new DeferralManager();
            dm.DeferralSource.GetDeferral().Dispose();
            IDisposable deferral = dm.DeferralSource.GetDeferral();
            Task task = dm.WaitForDeferralsAsync();
            await AsyncAssert.NeverCompletesAsync(task).ConfigureAwait(false);
        }
    }
}
