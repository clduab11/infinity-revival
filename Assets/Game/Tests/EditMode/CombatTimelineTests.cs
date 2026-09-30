using System;
using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class CombatTimelineTests
    {
        [Test]
        public void InitialInstantAcceptsEventsAndSealsAfterAdvance()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            Assert.That(timeline.TimeUs, Is.Zero);
            Assert.That(timeline.TryEnqueue(Command(1), 0), Is.True);
            Assert.That(timeline.TrySchedule(Milestone(1, 0)), Is.True);

            timeline.AdvanceTo(0);

            Assert.That(resolved.Count, Is.EqualTo(2));
            Assert.That(timeline.PendingCount, Is.Zero);
            Assert.That(timeline.TryEnqueue(Command(2), 0), Is.False);
            Assert.That(timeline.TrySchedule(Milestone(2, 0)), Is.False);
        }

        [Test]
        public void MixedEventsResolveChronologicallyAtTheirExactDueTimes()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            var callbackTimes = new List<long>();
            timeline.Resolved += value => callbackTimes.Add(timeline.TimeUs);
            timeline.TrySchedule(Milestone(1, 300));
            timeline.TryEnqueue(Command(1), 100);
            timeline.TrySchedule(Milestone(2, 200, CombatMilestoneKind.Telegraph));
            timeline.TryEnqueue(Command(2), 400);

            timeline.AdvanceTo(350);

            Assert.That(resolved.ConvertAll(value => value.TimeUs),
                Is.EqualTo(new long[] { 100, 200, 300 }));
            Assert.That(callbackTimes, Is.EqualTo(new long[] { 100, 200, 300 }));
            Assert.That(timeline.TimeUs, Is.EqualTo(350));
            Assert.That(timeline.PendingCount, Is.EqualTo(1));
        }

        [Test]
        public void ParryAtImpactInstantResolvesBeforeImpact()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            timeline.TrySchedule(Milestone(1, 100));
            timeline.TryEnqueue(Command(1), 100);

            timeline.AdvanceTo(100);

            Assert.That(resolved[0].Kind, Is.EqualTo(CombatEventKind.Command));
            Assert.That(resolved[0].Command.Value.Intent, Is.EqualTo(GestureIntent.Parry));
            Assert.That(resolved[1].Milestone.Value.Kind, Is.EqualTo(CombatMilestoneKind.Impact));
        }

        [Test]
        public void TiedCommandsPreserveArrivalDespiteReversedSequenceAndIntent()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            timeline.TryEnqueue(Command(90, GestureIntent.Attack), 100);
            timeline.TrySchedule(Milestone(1, 100));
            timeline.TryEnqueue(Command(2), 100);
            timeline.TryEnqueue(Command(1, GestureIntent.Attack), 100);

            timeline.AdvanceTo(100);

            Assert.That(resolved.ConvertAll(value => value.Command?.Sequence ?? -1),
                Is.EqualTo(new long[] { 90, 2, 1, -1 }));
            Assert.That(resolved[0].ArrivalOrder, Is.LessThan(resolved[1].ArrivalOrder));
            Assert.That(resolved[1].ArrivalOrder, Is.LessThan(resolved[2].ArrivalOrder));
        }

        [Test]
        public void TiedMilestonesAndCommandsFollowExplicitPriority()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            timeline.TrySchedule(Milestone(1, 100, CombatMilestoneKind.BufferedOffense));
            timeline.TrySchedule(Milestone(2, 100, CombatMilestoneKind.RecoveryComplete));
            timeline.TrySchedule(Milestone(3, 100, CombatMilestoneKind.PhaseTransition));
            timeline.TrySchedule(Milestone(4, 100, CombatMilestoneKind.Impact));
            timeline.TrySchedule(Milestone(5, 100, CombatMilestoneKind.Telegraph));
            timeline.TryEnqueue(Command(1), 100);
            timeline.TrySchedule(Milestone(6, 100, CombatMilestoneKind.Death));

            timeline.AdvanceTo(100);

            Assert.That(resolved.ConvertAll(value => value.Milestone?.Id ?? 0),
                Is.EqualTo(new long[] { 6, 0, 5, 4, 3, 2, 1 }));
        }

        [Test]
        public void SameMilestoneKindUsesArrivalOrderRatherThanIdentity()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            timeline.TrySchedule(Milestone(90, 100));
            timeline.TrySchedule(Milestone(1, 100));

            timeline.AdvanceTo(100);

            Assert.That(resolved.ConvertAll(value => value.Milestone.Value.Id),
                Is.EqualTo(new long[] { 90, 1 }));
            Assert.That(resolved[0].ArrivalOrder, Is.LessThan(resolved[1].ArrivalOrder));
        }

        [TestCase(99)]
        [TestCase(100)]
        public void SealedHorizonRejectsLateOrEqualEventsWithoutQueueChanges(long time)
        {
            var timeline = new CombatTimeline();
            timeline.AdvanceTo(100);
            timeline.TrySchedule(Milestone(1, 101));

            Assert.That(timeline.TryEnqueue(Command(1), time), Is.False);
            Assert.That(timeline.TrySchedule(Milestone(2, time)), Is.False);
            Assert.That(timeline.PendingCount, Is.EqualTo(1));
            Assert.That(timeline.TryEnqueue(Command(2), 101), Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void CapacityMustBePositive(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatTimeline(capacity));
        }

        [Test]
        public void FullQueueRejectsBothKindsWithoutOverwritingAndReusesFreedCapacity()
        {
            var timeline = new CombatTimeline(2);
            var resolved = Capture(timeline);
            timeline.TryEnqueue(Command(1), 100);
            timeline.TrySchedule(Milestone(1, 200));

            Assert.That(timeline.TryEnqueue(Command(2), 300), Is.False);
            Assert.That(timeline.TrySchedule(Milestone(2, 300)), Is.False);
            timeline.AdvanceTo(100);
            Assert.That(timeline.TrySchedule(Milestone(2, 300)), Is.True);
            timeline.AdvanceTo(300);
            Assert.That(resolved.ConvertAll(value => value.Milestone?.Id ?? 0),
                Is.EqualTo(new long[] { 0, 1, 2 }));
        }

        [Test]
        public void DuplicatePendingMilestoneIdentityIsRejectedAndReleasedOnResolution()
        {
            var timeline = new CombatTimeline();
            timeline.TrySchedule(Milestone(1, 100));

            Assert.That(timeline.TrySchedule(Milestone(1, 200)), Is.False);
            Assert.That(timeline.PendingCount, Is.EqualTo(1));
            timeline.AdvanceTo(100);
            Assert.That(timeline.TrySchedule(Milestone(1, 200)), Is.True);
        }

        [Test]
        public void ReentrantAdvanceThrowsWithoutChangingTheOuterAdvance()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            timeline.TrySchedule(Milestone(1, 100));
            timeline.TrySchedule(Milestone(2, 200));
            timeline.Resolved += value =>
            {
                Assert.Throws<InvalidOperationException>(() => timeline.AdvanceTo(999));
                Assert.That(timeline.TimeUs, Is.EqualTo(value.TimeUs));
            };

            timeline.AdvanceTo(300);

            Assert.That(resolved.Count, Is.EqualTo(2));
            Assert.That(timeline.TimeUs, Is.EqualTo(300));
        }

        [Test]
        public void CallbackRejectsRetroactiveAndCurrentEventsButCanAddFutureEvents()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            timeline.TrySchedule(Milestone(1, 100));
            timeline.Resolved += value =>
            {
                if (value.TimeUs != 100) return;
                Assert.That(timeline.TryEnqueue(Command(1), 100), Is.False);
                Assert.That(timeline.TryEnqueue(Command(2), 99), Is.False);
                Assert.That(timeline.TrySchedule(Milestone(2, 100)), Is.False);
                Assert.That(timeline.TrySchedule(Milestone(3, 99)), Is.False);
                Assert.That(timeline.TryEnqueue(Command(3), 101), Is.True);
                Assert.That(timeline.TrySchedule(Milestone(4, 102)), Is.True);
            };

            timeline.AdvanceTo(200);

            Assert.That(resolved.ConvertAll(value => value.TimeUs),
                Is.EqualTo(new long[] { 100, 101, 102 }));
            Assert.That(timeline.TimeUs, Is.EqualTo(200));
        }

        [Test]
        public void RegressingOrNegativeAdvanceLeavesClockAndQueueUnchanged()
        {
            var timeline = new CombatTimeline();
            timeline.TrySchedule(Milestone(1, 200));
            timeline.AdvanceTo(100);

            Assert.Throws<ArgumentOutOfRangeException>(() => timeline.AdvanceTo(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => timeline.AdvanceTo(99));
            Assert.That(timeline.TimeUs, Is.EqualTo(100));
            Assert.That(timeline.PendingCount, Is.EqualTo(1));
        }

        [Test]
        public void WarningShiftsAllRemainingMilestonesUniformlyAndDiscardsCommands()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            timeline.AdvanceTo(100);
            timeline.TrySchedule(Milestone(10, 120, CombatMilestoneKind.Telegraph));
            timeline.TryEnqueue(Command(1), 130);
            timeline.TrySchedule(Milestone(11, 150));
            timeline.TrySchedule(Milestone(12, 190, CombatMilestoneKind.PhaseTransition));
            timeline.TrySchedule(Milestone(13, 210, CombatMilestoneKind.RecoveryComplete));
            timeline.TrySchedule(Milestone(14, 220, CombatMilestoneKind.BufferedOffense));
            timeline.TrySchedule(Milestone(15, 170, CombatMilestoneKind.Death));

            Assert.That(timeline.EnsureIncomingWarning(), Is.EqualTo(650100));
            Assert.That(timeline.TimeUs, Is.EqualTo(100));
            Assert.That(timeline.PendingCount, Is.EqualTo(6));
            timeline.AdvanceTo(650200);
            Assert.That(resolved.ConvertAll(value => value.TimeUs),
                Is.EqualTo(new long[] { 650070, 650100, 650120, 650140, 650160, 650170 }));
            Assert.That(resolved.ConvertAll(value => value.Milestone.Value.Id),
                Is.EqualTo(new long[] { 10, 11, 15, 12, 13, 14 }));
            Assert.That(resolved.ConvertAll(value => value.ArrivalOrder),
                Is.EqualTo(new long[] { 1, 3, 7, 4, 5, 6 }));
            Assert.That(resolved[0].Milestone.Value.Kind, Is.EqualTo(CombatMilestoneKind.Telegraph));
            Assert.That(resolved[5].Milestone.Value.Kind, Is.EqualTo(CombatMilestoneKind.BufferedOffense));
        }

        [Test]
        public void WarningUsesEarliestPendingImpactAndDoesNotShiftSufficientWarning()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            timeline.TrySchedule(Milestone(1, 1000));
            timeline.TrySchedule(Milestone(2, 800));
            timeline.TryEnqueue(Command(1), 700);

            Assert.That(timeline.EnsureIncomingWarning(800), Is.EqualTo(800));
            Assert.That(timeline.PendingCount, Is.EqualTo(2));
            timeline.AdvanceTo(1000);
            Assert.That(resolved.ConvertAll(value => value.TimeUs),
                Is.EqualTo(new long[] { 800, 1000 }));
        }

        [Test]
        public void WarningWithoutImpactStillClearsPendingCommands()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            timeline.TryEnqueue(Command(1), 100);
            timeline.TrySchedule(Milestone(1, 200, CombatMilestoneKind.RecoveryComplete));

            Assert.That(timeline.EnsureIncomingWarning(), Is.EqualTo(-1));
            Assert.That(timeline.PendingCount, Is.EqualTo(1));
            timeline.AdvanceTo(200);
            Assert.That(resolved[0].Milestone.Value.Id, Is.EqualTo(1));
        }

        [Test]
        public void DiscardCommandsPreservesMilestonesAndFreesCapacity()
        {
            var timeline = new CombatTimeline(2);
            var resolved = Capture(timeline);
            timeline.TryEnqueue(Command(1), 100);
            timeline.TrySchedule(Milestone(1, 200));

            timeline.DiscardCommands();

            Assert.That(timeline.PendingCount, Is.EqualTo(1));
            Assert.That(timeline.TryEnqueue(Command(2), 300), Is.True);
            timeline.AdvanceTo(300);
            Assert.That(resolved[0].Milestone.Value.Id, Is.EqualTo(1));
            Assert.That(resolved[1].Command.Value.Sequence, Is.EqualTo(2));
        }

        [Test]
        public void OverflowDuringWarningLeavesAllTimesAndPendingCommandsUnchanged()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            timeline.AdvanceTo(100);
            timeline.TrySchedule(Milestone(1, 101));
            timeline.TryEnqueue(Command(1), 200);
            timeline.TrySchedule(Milestone(2, long.MaxValue, CombatMilestoneKind.RecoveryComplete));

            Assert.Throws<OverflowException>(() => timeline.EnsureIncomingWarning(650));
            Assert.That(timeline.PendingCount, Is.EqualTo(3));
            Assert.That(timeline.TimeUs, Is.EqualTo(100));
            timeline.AdvanceTo(long.MaxValue);
            Assert.That(resolved.ConvertAll(value => value.TimeUs),
                Is.EqualTo(new long[] { 101, 200, long.MaxValue }));
        }

        [Test]
        public void NegativeWarningLeavesPendingCommandsUnchanged()
        {
            var timeline = new CombatTimeline();
            timeline.TryEnqueue(Command(1), 100);

            Assert.Throws<ArgumentOutOfRangeException>(() => timeline.EnsureIncomingWarning(-1));
            Assert.That(timeline.PendingCount, Is.EqualTo(1));
        }

        [Test]
        public void EventPayloadsAreExclusiveAndPreserveCapturedInputTimestamp()
        {
            var timeline = new CombatTimeline();
            var resolved = Capture(timeline);
            timeline.TryEnqueue(Command(1), 100);
            timeline.TrySchedule(Milestone(1, 200));

            timeline.AdvanceTo(200);

            Assert.That(resolved[0].Command.Value.InputTimestampUs, Is.EqualTo(500));
            Assert.That(resolved[0].Milestone.HasValue, Is.False);
            Assert.That(resolved[1].Command.HasValue, Is.False);
            Assert.That(resolved[1].Kind, Is.EqualTo(CombatEventKind.Milestone));
            Assert.That(resolved[1].Milestone.Value.TimeUs, Is.EqualTo(200));
        }

        [TestCase(0, 100, CombatMilestoneKind.Impact)]
        [TestCase(-1, 100, CombatMilestoneKind.Impact)]
        [TestCase(1, -1, CombatMilestoneKind.Impact)]
        [TestCase(1, 100, (CombatMilestoneKind)99)]
        public void MilestoneRejectsMalformedIdentityTimeOrKind(
            long id, long time, CombatMilestoneKind kind)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatMilestone(id, time, kind));
        }

        [Test]
        public void QueueRejectsDefaultValuesAndNegativeCommandTime()
        {
            var timeline = new CombatTimeline();

            Assert.Throws<ArgumentOutOfRangeException>(() => timeline.TryEnqueue(default(GestureCommand), 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => timeline.TrySchedule(default(CombatMilestone)));
            Assert.Throws<ArgumentOutOfRangeException>(() => timeline.TryEnqueue(Command(1), -1));
            Assert.That(timeline.PendingCount, Is.Zero);
        }

        private static List<CombatTimelineEvent> Capture(CombatTimeline timeline)
        {
            var events = new List<CombatTimelineEvent>();
            timeline.Resolved += events.Add;
            return events;
        }

        private static CombatMilestone Milestone(long id, long time,
            CombatMilestoneKind kind = CombatMilestoneKind.Impact)
        {
            return new CombatMilestone(id, time, kind);
        }

        private static GestureCommand Command(long sequence,
            GestureIntent intent = GestureIntent.Parry)
        {
            var phaseKind = intent == GestureIntent.Parry
                ? InteractionPhaseKind.EnemySequence : InteractionPhaseKind.PlayerOpening;
            return new GestureCommand(sequence, 500, 1, SwipeDirection.Right, intent,
                new InteractionPhase(1, phaseKind), new NormalizedPoint(0, 0),
                new NormalizedPoint(0.1, 0));
        }
    }
}
