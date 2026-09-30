using System;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class CombatClockTests
    {
        [Test]
        public void NonzeroDeviceOriginStartsAtZeroCombatTimeAndMapsWithinTheRunningSegment()
        {
            var clock = new CombatClock(1000000);

            AssertSnapshot(clock, 0, 1000000, CombatClockState.Running,
                CombatSuspensionReason.None, 0);
            clock.Advance(1100000);

            Assert.That(clock.TimeUs, Is.EqualTo(100000));
            Assert.That(clock.TryMapTimestamp(1050000, out long mapped), Is.True);
            Assert.That(mapped, Is.EqualTo(50000));
        }

        [Test]
        public void NegativeDeviceOriginIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatClock(-1));
        }

        [Test]
        public void ExactlyOneHundredFiftyMillisecondsIsAccepted()
        {
            var clock = new CombatClock(0);

            Assert.That(clock.CheckForStall(150000), Is.False);
            clock.Advance(150000);

            AssertSnapshot(clock, 150000, 150000, CombatClockState.Running,
                CombatSuspensionReason.None, 0);
        }

        [Test]
        public void OneHundredFiftyOneMillisecondsSuspendsWithoutAcceptingTheGap()
        {
            var clock = new CombatClock(0);

            Assert.That(clock.CheckForStall(151000), Is.True);

            AssertSnapshot(clock, 0, 151000, CombatClockState.Suspended,
                CombatSuspensionReason.FrameStall, 0);
        }

        [Test]
        public void AdvanceDetectsAStallWithoutAdvancingCombatTime()
        {
            var clock = new CombatClock(0);
            clock.Advance(100000);

            clock.Advance(251000);

            AssertSnapshot(clock, 100000, 251000, CombatClockState.Suspended,
                CombatSuspensionReason.FrameStall, 0);
        }

        [Test]
        public void CheckingANormalGapDoesNotConsumeIt()
        {
            var clock = new CombatClock(0);

            Assert.That(clock.CheckForStall(100000), Is.False);
            AssertSnapshot(clock, 0, 0, CombatClockState.Running,
                CombatSuspensionReason.None, 0);
            clock.Advance(100000);

            Assert.That(clock.TimeUs, Is.EqualTo(100000));
        }

        [Test]
        public void RepeatingTheSameDeviceTimestampAddsNoTime()
        {
            var clock = new CombatClock(0);
            clock.Advance(100000);

            clock.Advance(100000);

            Assert.That(clock.TimeUs, Is.EqualTo(100000));
        }

        [Test]
        public void SuspensionFreezesAtTheLastAcceptedTimeAndInvalidatesMapping()
        {
            var clock = new CombatClock(0);
            clock.Advance(100000);
            Assert.That(clock.TryMapTimestamp(50000, out long beforePause), Is.True);
            Assert.That(beforePause, Is.EqualTo(50000));

            clock.Suspend(120000, CombatSuspensionReason.FocusLost);

            AssertSnapshot(clock, 100000, 120000, CombatClockState.Suspended,
                CombatSuspensionReason.FocusLost, 0);
            Assert.That(clock.TryMapTimestamp(50000, out long afterPause), Is.False);
            Assert.That(afterPause, Is.Zero);
        }

        [Test]
        public void SuspendedAdvanceOnlyUpdatesTheDeviceTimestamp()
        {
            var clock = new CombatClock(0);
            clock.Advance(100000);
            clock.Suspend(120000, CombatSuspensionReason.ApplicationPaused);

            Assert.That(clock.CheckForStall(5000000), Is.False);
            Assert.That(clock.LastDeviceTimeUs, Is.EqualTo(120000));
            clock.Advance(5000000);

            AssertSnapshot(clock, 100000, 5000000, CombatClockState.Suspended,
                CombatSuspensionReason.ApplicationPaused, 0);
        }

        [Test]
        public void ResumeUsesAThreeSecondCountdownByDefault()
        {
            var clock = new CombatClock(0);
            clock.Suspend(0, CombatSuspensionReason.FocusLost);

            clock.BeginResume(0);
            AssertSnapshot(clock, 0, 0, CombatClockState.Countdown,
                CombatSuspensionReason.FocusLost, 3000000);
            clock.Advance(150000);

            AssertSnapshot(clock, 0, 150000, CombatClockState.Countdown,
                CombatSuspensionReason.FocusLost, 2850000);
        }

        [Test]
        public void CountdownOvershootIsDiscardedBeforeCombatAdvancesAgain()
        {
            var clock = new CombatClock(0);
            clock.Advance(100000);
            clock.Suspend(100000, CombatSuspensionReason.FocusLost);
            clock.BeginResume(100000, 200000);
            clock.Advance(250000);
            Assert.That(clock.CountdownRemainingUs, Is.EqualTo(50000));

            clock.Advance(350000);

            AssertSnapshot(clock, 100000, 350000, CombatClockState.Running,
                CombatSuspensionReason.None, 0);
            Assert.That(clock.TryMapTimestamp(350000, out long boundary), Is.True);
            Assert.That(boundary, Is.EqualTo(100000));
            clock.Advance(360000);
            Assert.That(clock.TimeUs, Is.EqualTo(110000));
        }

        [Test]
        public void AStallDuringCountdownSuspendsAndClearsItsRemainingTime()
        {
            var clock = new CombatClock(0);
            clock.Advance(100000);
            clock.Suspend(100000, CombatSuspensionReason.FocusLost);
            clock.BeginResume(100000, 400000);

            clock.Advance(251000);

            AssertSnapshot(clock, 100000, 251000, CombatClockState.Suspended,
                CombatSuspensionReason.FrameStall, 0);
        }

        [Test]
        public void CountdownDoesNotMapInputTimestamps()
        {
            var clock = new CombatClock(0);
            clock.Advance(100000);
            clock.Suspend(100000, CombatSuspensionReason.FocusLost);
            clock.BeginResume(100000, 200000);
            clock.Advance(150000);

            Assert.That(clock.TryMapTimestamp(150000, out long mapped), Is.False);
            Assert.That(mapped, Is.Zero);
        }

        [Test]
        public void ResumingStartsANewMappingSegmentWithTheExistingCombatTime()
        {
            var clock = new CombatClock(1000000);
            clock.Advance(1100000);
            clock.Suspend(1120000, CombatSuspensionReason.ApplicationPaused);
            clock.BeginResume(2000000, 100000);
            clock.Advance(2100000);
            clock.Advance(2150000);

            Assert.That(clock.TryMapTimestamp(1100000, out long stale), Is.False);
            Assert.That(stale, Is.Zero);
            Assert.That(clock.TryMapTimestamp(2110000, out long current), Is.True);
            Assert.That(current, Is.EqualTo(110000));
            Assert.That(clock.TimeUs, Is.EqualTo(150000));
        }

        [TestCase(-1)]
        [TestCase(999999)]
        [TestCase(1100001)]
        public void NegativeStaleAndFutureTimestampsFailWithoutClamping(long sourceTimestamp)
        {
            var clock = new CombatClock(1000000);
            clock.Advance(1100000);

            Assert.That(clock.TryMapTimestamp(sourceTimestamp, out long mapped), Is.False);
            Assert.That(mapped, Is.Zero);
            Assert.That(clock.TimeUs, Is.EqualTo(100000));
        }

        [Test]
        public void RepeatedSuspensionKeepsCombatTimeFrozenAndRecordsTheLatestReason()
        {
            var clock = new CombatClock(0);
            clock.Advance(100000);
            clock.Suspend(120000, CombatSuspensionReason.FocusLost);

            clock.Suspend(200000, CombatSuspensionReason.ApplicationPaused);

            AssertSnapshot(clock, 100000, 200000, CombatClockState.Suspended,
                CombatSuspensionReason.ApplicationPaused, 0);
        }

        [Test]
        public void ExplicitSuspensionDuringCountdownClearsTheCountdown()
        {
            var clock = new CombatClock(0);
            clock.Suspend(0, CombatSuspensionReason.FocusLost);
            clock.BeginResume(0, 200000);
            clock.Advance(100000);

            clock.Suspend(120000, CombatSuspensionReason.ApplicationPaused);

            AssertSnapshot(clock, 0, 120000, CombatClockState.Suspended,
                CombatSuspensionReason.ApplicationPaused, 0);
        }

        [TestCase(-1)]
        [TestCase(49)]
        public void InvalidDeviceTimesLeaveRunningStateUnchanged(long deviceNow)
        {
            var clock = new CombatClock(0);
            clock.Advance(50);

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.CheckForStall(deviceNow));
            AssertSnapshot(clock, 50, 50, CombatClockState.Running,
                CombatSuspensionReason.None, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(deviceNow));
            AssertSnapshot(clock, 50, 50, CombatClockState.Running,
                CombatSuspensionReason.None, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                clock.Suspend(deviceNow, CombatSuspensionReason.FocusLost));
            AssertSnapshot(clock, 50, 50, CombatClockState.Running,
                CombatSuspensionReason.None, 0);
        }

        [TestCase(-1)]
        [TestCase(99)]
        public void InvalidResumeDeviceTimesLeaveSuspensionUnchanged(long deviceNow)
        {
            var clock = new CombatClock(0);
            clock.Advance(50);
            clock.Suspend(100, CombatSuspensionReason.FocusLost);

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.BeginResume(deviceNow, 100));

            AssertSnapshot(clock, 50, 100, CombatClockState.Suspended,
                CombatSuspensionReason.FocusLost, 0);
        }

        [TestCase(CombatSuspensionReason.None)]
        [TestCase((CombatSuspensionReason)99)]
        public void InvalidSuspensionReasonsDoNotChangeTheRunningClock(CombatSuspensionReason reason)
        {
            var clock = new CombatClock(0);
            clock.Advance(50);

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Suspend(60, reason));

            AssertSnapshot(clock, 50, 50, CombatClockState.Running,
                CombatSuspensionReason.None, 0);
            Assert.That(clock.TryMapTimestamp(50, out long mapped), Is.True);
            Assert.That(mapped, Is.EqualTo(50));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonpositiveCountdownsLeaveSuspensionUnchanged(long countdownUs)
        {
            var clock = new CombatClock(0);
            clock.Suspend(100, CombatSuspensionReason.FocusLost);

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.BeginResume(120, countdownUs));

            AssertSnapshot(clock, 0, 100, CombatClockState.Suspended,
                CombatSuspensionReason.FocusLost, 0);
        }

        [Test]
        public void BeginningResumeWhileRunningIsRejectedWithoutChangingState()
        {
            var clock = new CombatClock(0);
            clock.Advance(50);

            Assert.Throws<InvalidOperationException>(() => clock.BeginResume(60, 100));

            AssertSnapshot(clock, 50, 50, CombatClockState.Running,
                CombatSuspensionReason.None, 0);
        }

        [Test]
        public void BeginningResumeDuringCountdownIsRejectedWithoutRestartingIt()
        {
            var clock = new CombatClock(0);
            clock.Suspend(0, CombatSuspensionReason.FocusLost);
            clock.BeginResume(0, 100000);
            clock.Advance(50000);

            Assert.Throws<InvalidOperationException>(() => clock.BeginResume(60000, 200000));

            AssertSnapshot(clock, 0, 50000, CombatClockState.Countdown,
                CombatSuspensionReason.FocusLost, 50000);
        }

        [Test]
        public void CountdownDeadlineOverflowLeavesAllStateUnchanged()
        {
            var clock = new CombatClock(long.MaxValue - 50);
            clock.Advance(long.MaxValue - 40);
            clock.Suspend(long.MaxValue - 30, CombatSuspensionReason.FocusLost);

            Assert.Throws<OverflowException>(() => clock.BeginResume(long.MaxValue - 20, 30));

            AssertSnapshot(clock, 10, long.MaxValue - 30, CombatClockState.Suspended,
                CombatSuspensionReason.FocusLost, 0);
        }

        [Test]
        public void DeviceTimesAtTheSignedIntegerLimitKeepSmallDeltasExact()
        {
            var clock = new CombatClock(long.MaxValue - 100);

            clock.Advance(long.MaxValue);
            clock.Advance(long.MaxValue);

            AssertSnapshot(clock, 100, long.MaxValue, CombatClockState.Running,
                CombatSuspensionReason.None, 0);
            Assert.That(clock.TryMapTimestamp(long.MaxValue - 25, out long mapped), Is.True);
            Assert.That(mapped, Is.EqualTo(75));
        }

        [Test]
        public void ResumedMappingNearTheSignedIntegerLimitAddsOnlyTheSegmentDelta()
        {
            var clock = new CombatClock(long.MaxValue - 500);
            clock.Advance(long.MaxValue - 450);
            clock.Suspend(long.MaxValue - 400, CombatSuspensionReason.FocusLost);
            clock.BeginResume(long.MaxValue - 300, 100);
            clock.Advance(long.MaxValue - 200);
            clock.Advance(long.MaxValue - 100);

            AssertSnapshot(clock, 150, long.MaxValue - 100, CombatClockState.Running,
                CombatSuspensionReason.None, 0);
            Assert.That(clock.TryMapTimestamp(long.MaxValue - 150, out long mapped), Is.True);
            Assert.That(mapped, Is.EqualTo(100));
        }

        private static void AssertSnapshot(CombatClock clock, long time, long deviceTime,
            CombatClockState state, CombatSuspensionReason reason, long countdown)
        {
            Assert.That(clock.TimeUs, Is.EqualTo(time));
            Assert.That(clock.LastDeviceTimeUs, Is.EqualTo(deviceTime));
            Assert.That(clock.State, Is.EqualTo(state));
            Assert.That(clock.SuspensionReason, Is.EqualTo(reason));
            Assert.That(clock.CountdownRemainingUs, Is.EqualTo(countdown));
        }
    }
}
