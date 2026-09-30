using System;
using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Application.Combat;
using Praxen.Game.Application.Input;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class EnemySequenceAdmissionTests
    {
        [Test]
        public void ReusedMilestoneValuesHaveDistinctAdmissionIdentity()
        {
            var timeline = new CombatTimeline();
            var milestone = new CombatMilestone(99, 100000, CombatMilestoneKind.PhaseTransition);
            Assert.That(timeline.TrySchedule(milestone), Is.True);
            Assert.That(timeline.TryGetMilestoneRecord(99, out var original), Is.True);
            Assert.That(timeline.TryCancelMilestone(99), Is.True);
            Assert.That(timeline.TrySchedule(milestone), Is.True);
            Assert.That(timeline.TryGetMilestoneRecord(99, out var replacement), Is.True);
            Assert.That(replacement.ArrivalOrder, Is.GreaterThan(original.ArrivalOrder));
            Assert.That(replacement.Milestone.Value.TimeUs, Is.EqualTo(original.Milestone.Value.TimeUs));
        }

        [Test]
        public void SequenceReservesEveryStrikeAndOneFinalOpeningAtomically()
        {
            using var rig = new Rig();
            var impacts = new List<long>();
            rig.Encounter.StrikeResolved += r => impacts.Add(r.TimeUs);
            Assert.That(rig.Duel.CommitSequence(Sequence()), Is.True);
            Assert.That(rig.Session.Timeline.PendingCount, Is.EqualTo(7));
            rig.AdvanceTo(300000);
            Assert.That(rig.Duel.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.EnemySequence));
            rig.AdvanceTo(600000);
            Assert.That(impacts, Is.EqualTo(new long[] { 100000, 400000 }));
            Assert.That(rig.Duel.Offense.OpeningStartUs, Is.EqualTo(600000));
            Assert.That(rig.Duel.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.PlayerOpening));
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(6)]
        public void RejectedSequenceDoesNotPartiallyAdmitOrCancelForeignRecords(int available)
        {
            using var rig = new Rig(7);
            for (int i = 0; i < 7 - available; i++)
                rig.Session.Timeline.TrySchedule(new CombatMilestone(100 + i, 5000000,
                    CombatMilestoneKind.Telegraph));
            Assert.That(rig.Duel.CommitSequence(Sequence()), Is.False);
            Assert.That(rig.Encounter.HasCommittedStrike, Is.False);
            Assert.That(rig.Session.Timeline.PendingCount, Is.EqualTo(7 - available));
        }

        [Test]
        public void ExtraMarkerCollisionRejectsWholeBaseBatch()
        {
            using var rig = new Rig();
            Assert.That(rig.Encounter.CommitSequence(Sequence(),
                new CombatMilestone(4, 600000, CombatMilestoneKind.PhaseTransition)), Is.False);
            Assert.That(rig.Encounter.HasCommittedStrike, Is.False);
            Assert.That(rig.Session.Timeline.PendingCount, Is.Zero);
        }

        [Test]
        public void DuplicateOrOverlappingStepsAreRejectedBeforeAdmission()
        {
            using var rig = new Rig();
            Assert.That(() => rig.Duel.CommitSequence(new[] {
                new ScheduledEnemyStrike(Strike(1), 0), new ScheduledEnemyStrike(Strike(1), 300000) }),
                Throws.ArgumentException);
            Assert.That(() => rig.Duel.CommitSequence(new[] {
                new ScheduledEnemyStrike(Strike(1), 0), new ScheduledEnemyStrike(Strike(2), 299999) }),
                Throws.ArgumentException);
            Assert.That(rig.Session.Timeline.PendingCount, Is.Zero);
            Assert.That(rig.Encounter.HasCommittedStrike, Is.False);
        }

        [Test]
        public void OverflowAndDefaultValuesDoNotMutateTimeline()
        {
            using var rig = new Rig();
            Assert.That(() => new ScheduledEnemyStrike(Strike(1), long.MaxValue),
                Throws.TypeOf<OverflowException>());
            Assert.That(() => rig.Duel.CommitSequence(new[] { default(ScheduledEnemyStrike) }),
                Throws.ArgumentException);
            Assert.That(() => rig.Duel.CommitSequence(new[] {
                new ScheduledEnemyStrike(Strike(long.MaxValue / 3 + 1), 0) }),
                Throws.TypeOf<OverflowException>());
            Assert.That(rig.Session.Timeline.PendingCount, Is.Zero);
        }

        [Test]
        public void ZeroFinalRecoveryStillResolvesInclusiveParryBeforeOpening()
        {
            using var rig = new Rig();
            var sequence = new[] { new ScheduledEnemyStrike(Strike(1), 0),
                new ScheduledEnemyStrike(Strike(2, 0), 300000) };
            rig.Duel.CommitSequence(sequence);
            var command = new GestureCommand(1, Rig.Origin + 400000, 1, SwipeDirection.Up,
                GestureIntent.Parry, rig.Duel.CurrentPhase, new NormalizedPoint(.5, .5),
                new NormalizedPoint(.5, .7));
            rig.AdvanceTo(300000);
            rig.Session.AdvanceFrame(Rig.Origin + 400000, new[] { command });
            Assert.That(rig.Duel.Momentum.Focus, Is.EqualTo(25));
            Assert.That(rig.Duel.Offense.OpeningStartUs, Is.EqualTo(400000));
        }

        [Test]
        public void ResumeRetimesAllStepsAndOnlyFinalOpening()
        {
            using var rig = new Rig();
            var impacts = new List<long>();
            rig.Encounter.StrikeResolved += r => impacts.Add(r.TimeUs);
            rig.Duel.CommitSequence(Sequence());
            rig.AdvanceTo(50000);
            rig.Session.Suspend(Rig.Origin + 50000, CombatSuspensionReason.FocusLost);
            rig.Session.BeginResume(Rig.Origin + 50000, 100000);
            rig.Session.AdvanceFrame(Rig.Origin + 150000, Array.Empty<GestureCommand>());
            rig.DeviceOffset = 100000;
            rig.AdvanceTo(1200000);
            Assert.That(impacts, Is.EqualTo(new long[] { 700000, 1000000 }));
            Assert.That(rig.Duel.Offense.OpeningStartUs, Is.EqualTo(1200000));
        }

        private static ScheduledEnemyStrike[] Sequence() => new[] {
            new ScheduledEnemyStrike(Strike(1), 0), new ScheduledEnemyStrike(Strike(2), 300000) };
        private static EnemyStrike Strike(long id, long recovery = 200000) => new EnemyStrike(id,
            DefenseMask.Parry, DodgeSide.None, SwipeDirection.Up, 0, 0, 100000, recovery);

        private sealed class Rig : IDisposable
        {
            internal const long Origin = 5000000;
            internal readonly CombatSession Session;
            internal readonly CombatEncounter Encounter;
            internal readonly CombatOpeningController Duel;
            internal long DeviceOffset;
            internal Rig(int capacity = 1024)
            {
                Session = new CombatSession(Origin, capacity);
                var phase = new InteractionPhase(1, InteractionPhaseKind.EnemySequence);
                var phases = new GesturePhaseContext();
                phases.SetPhase(phase);
                Encounter = new CombatEncounter(Session, phase);
                Duel = new CombatOpeningController(Encounter, phases);
            }
            internal void AdvanceTo(long target)
            {
                while (Session.Clock.TimeUs < target)
                    Session.AdvanceFrame(Origin + DeviceOffset + Math.Min(target,
                        Session.Clock.TimeUs + 100000), Array.Empty<GestureCommand>());
            }
            public void Dispose() { Duel.Dispose(); Encounter.Dispose(); }
        }
    }
}
