using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Praxen.Game.Application.Combat;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class DefenseEncounterTests
    {
        [Test]
        public void DisposeCancelsOnlyOwnedStrikeMilestonesAndAllowsReuse()
        {
            var session = new CombatSession(0);
            var phase = new InteractionPhase(1, InteractionPhaseKind.EnemySequence);
            var encounter = new CombatEncounter(session, phase);
            Assert.That(session.Timeline.TrySchedule(new CombatMilestone(99, 2000000,
                CombatMilestoneKind.PhaseTransition)), Is.True);
            Assert.That(encounter.CommitStrike(Strike(1), 0), Is.True);
            encounter.Dispose();
            Assert.That(session.Timeline.PendingCount, Is.EqualTo(1));
            using (var replacement = new CombatEncounter(session, phase))
                Assert.That(replacement.CommitStrike(Strike(1), 0), Is.True);
            Assert.That(session.Timeline.PendingCount, Is.EqualTo(1));
        }
        private static readonly InteractionPhase Phase = new InteractionPhase(1, InteractionPhaseKind.EnemySequence);
        private static GestureCommand Parry(long sequence, long sourceUs, long phaseId = 1) =>
            new GestureCommand(sequence, sourceUs, 1, SwipeDirection.Right, GestureIntent.Parry,
                new InteractionPhase(phaseId, InteractionPhaseKind.EnemySequence),
                new NormalizedPoint(0.1, 0.2), new NormalizedPoint(0.4, 0.2));
        private static EnemyStrike Strike(long id = 1) => new EnemyStrike(id,
            DefenseMask.Guard | DefenseMask.Parry | DefenseMask.Dodge, DodgeSide.Both,
            SwipeDirection.Right, 20, 25);

        [Test]
        public void ResolvedMilestoneIdCanBeReusedWithoutRoutingOrDisposalByOldOwner()
        {
            var session = new CombatSession(0);
            var encounter = new CombatEncounter(session, Phase);
            int tells = 0;
            encounter.TelegraphStarted += _ => tells++;
            encounter.CommitStrike(Strike(), 0);
            session.AdvanceInputFrame(10000, new CombatInput[0]);
            Assert.That(tells, Is.EqualTo(1));
            session.Timeline.TrySchedule(new CombatMilestone(1, 100000, CombatMilestoneKind.Telegraph));
            session.AdvanceInputFrame(100000, new CombatInput[0]);
            Assert.That(tells, Is.EqualTo(1));
            session.Timeline.TrySchedule(new CombatMilestone(1, 200000, CombatMilestoneKind.PhaseTransition));
            encounter.Dispose();
            Assert.That(session.Timeline.PendingCount, Is.EqualTo(1));
        }

        [Test]
        public void StaleReleaseForOldGuardCannotClearAFreshContactsGuard()
        {
            var session = new CombatSession(0);
            using (var encounter = new CombatEncounter(session, Phase))
            {
                session.AdvanceInputFrame(100000, new[] { new CombatInput(
                    new DefenseCommand(1, 1000, DefenseCommandKind.GuardPress, guardActionId: 1)) });
                session.AdvanceInputFrame(110000, new[] {
                    new CombatInput(new DefenseCommand(2, 50000, DefenseCommandKind.GuardRelease, guardActionId: 1)),
                    new CombatInput(new DefenseCommand(3, 105000, DefenseCommandKind.GuardPress, guardActionId: 3)) });
                Assert.That(encounter.Player.GuardHeld, Is.True);
                Assert.That(session.RejectedCommandCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void RejectedReleaseAtCapacityCannotLeaveAnEarlierQueuedPressHeld()
        {
            var session = new CombatSession(0, 3);
            using (var encounter = new CombatEncounter(session, Phase))
            {
                encounter.CommitStrike(Strike(), 0);
                session.AdvanceInputFrame(10000, new CombatInput[0]);
                session.AdvanceInputFrame(100000, new[] {
                    new CombatInput(new DefenseCommand(1, 50000, DefenseCommandKind.GuardPress)),
                    new CombatInput(new DefenseCommand(2, 90000, DefenseCommandKind.GuardRelease)) });
                Assert.That(encounter.Player.GuardHeld, Is.False);
                Assert.That(session.RejectedCommandCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void RejectedTerminalReleaseClearsGuardWithoutRetroactiveImpactChanges()
        {
            var session = new CombatSession(0);
            using (var encounter = new CombatEncounter(session, Phase))
            {
                encounter.CommitStrike(Strike(), 0);
                session.AdvanceInputFrame(100000, new[] { new CombatInput(
                    new DefenseCommand(1, 1000, DefenseCommandKind.GuardPress)) });
                session.AdvanceInputFrame(110000, new[] { new CombatInput(
                    new DefenseCommand(2, 50000, DefenseCommandKind.GuardRelease)) });
                Assert.That(encounter.Player.GuardHeld, Is.False);
                Assert.That(session.RejectedCommandCount, Is.EqualTo(1));
                for (long t = 210000; t <= 610000; t += 100000)
                    session.AdvanceInputFrame(t, new CombatInput[0]);
                session.AdvanceInputFrame(650000, new CombatInput[0]);
                Assert.That(encounter.Player.Health, Is.EqualTo(75));
            }
        }

        [Test]
        public void RejectedGuardReleaseCannotDisarmAnAcceptedParry()
        {
            var session = new CombatSession(0);
            using (var encounter = new CombatEncounter(session, Phase))
            {
                encounter.CommitStrike(Strike(), 0);
                session.AdvanceInputFrame(100000, new[] { new CombatInput(
                    new DefenseCommand(1, 1000, DefenseCommandKind.GuardPress)) });
                for (long t = 200000; t <= 500000; t += 100000)
                    session.AdvanceInputFrame(t, new CombatInput[0]);
                session.AdvanceInputFrame(550000, new[] { new CombatInput(Parry(2, 520000)) });
                session.AdvanceInputFrame(560000, new[] { new CombatInput(
                    new DefenseCommand(3, 500000, DefenseCommandKind.GuardRelease)) });
                session.AdvanceInputFrame(650000, new CombatInput[0]);
                Assert.That(encounter.Player.Health, Is.EqualTo(100));
                Assert.That(encounter.Player.State, Is.EqualTo(PlayerCombatState.Recovery));
            }
        }

        [Test]
        public void MixedInputTiePreservesGlobalArrivalBeforeImpact()
        {
            var timeline = new CombatTimeline();
            var events = new List<CombatTimelineEvent>();
            timeline.Resolved += events.Add;
            timeline.TrySchedule(new CombatMilestone(1, 100, CombatMilestoneKind.Impact));
            timeline.TryEnqueue(new DefenseCommand(90, 100, DefenseCommandKind.GuardPress), 100);
            timeline.TryEnqueue(Parry(2, 100), 100);
            timeline.TryEnqueue(new DefenseCommand(1, 100, DefenseCommandKind.DodgeLeft), 100);
            timeline.AdvanceTo(100);
            Assert.That(events.Select(e => e.Kind), Is.EqualTo(new[] { CombatEventKind.DefenseCommand,
                CombatEventKind.Command, CombatEventKind.DefenseCommand, CombatEventKind.Milestone }));
            Assert.That(events[0].DefenseCommand.Value.Sequence, Is.EqualTo(90));
            Assert.That(events[2].DefenseCommand.Value.Sequence, Is.EqualTo(1));
        }

        [Test]
        public void AtomicSchedulingCannotLeaveAnOrphanTelegraphOnCapacityFailure()
        {
            var timeline = new CombatTimeline(2);
            Assert.That(timeline.TryScheduleBatch(new[] {
                new CombatMilestone(1, 0, CombatMilestoneKind.Telegraph),
                new CombatMilestone(2, 650000, CombatMilestoneKind.Impact),
                new CombatMilestone(3, 1150000, CombatMilestoneKind.RecoveryComplete) }), Is.False);
            Assert.That(timeline.PendingCount, Is.Zero);
            Assert.That(timeline.TrySchedule(new CombatMilestone(1, 0, CombatMilestoneKind.Telegraph)), Is.True);
        }

        [Test]
        public void AtomicSchedulingRejectsDuplicateIdsWithoutPartialMutation()
        {
            var timeline = new CombatTimeline();
            Assert.That(timeline.TryScheduleBatch(new[] {
                new CombatMilestone(1, 0, CombatMilestoneKind.Telegraph),
                new CombatMilestone(1, 650000, CombatMilestoneKind.Impact) }), Is.False);
            Assert.That(timeline.PendingCount, Is.Zero);
        }

        [Test]
        public void MixedInputsAreDiscardedTogetherWhenSuspended()
        {
            var timeline = new CombatTimeline();
            timeline.TryEnqueue(new DefenseCommand(1, 100, DefenseCommandKind.GuardPress), 100);
            timeline.TryEnqueue(Parry(2, 100), 100);
            timeline.TrySchedule(new CombatMilestone(1, 200, CombatMilestoneKind.Impact));
            timeline.DiscardCommands();
            Assert.That(timeline.PendingCount, Is.EqualTo(1));
            Assert.That(timeline.TryGetNextImpact(out var impact), Is.True);
            Assert.That(impact.TimeUs, Is.EqualTo(200));
        }

        [Test]
        public void ExactImpactParryResolvesThroughApplicationWithRecovery()
        {
            var session = new CombatSession(1000000);
            using (var encounter = new CombatEncounter(session, Phase))
            {
                var results = new List<DefenseResolution>();
                encounter.StrikeResolved += results.Add;
                Assert.That(encounter.CommitStrike(Strike(), 0), Is.True);
                for (long t = 1100000; t <= 1600000; t += 100000)
                    session.AdvanceFrame(t, new GestureCommand[0]);
                session.AdvanceInputFrame(1650000, new[] { new CombatInput(Parry(1, 1650000)) });
                Assert.That(results.Single().Outcome, Is.EqualTo(DefenseOutcome.Parried));
                Assert.That(encounter.Player.Health, Is.EqualTo(100));
                Assert.That(encounter.Player.State, Is.EqualTo(PlayerCombatState.Recovery));
                session.AdvanceFrame(1750000, new GestureCommand[0]);
                session.AdvanceFrame(1850000, new GestureCommand[0]);
                session.AdvanceFrame(1900000, new GestureCommand[0]);
                Assert.That(encounter.Player.State, Is.EqualTo(PlayerCombatState.Ready));
            }
        }

        [Test]
        public void CapturedStalePhaseCannotParryCurrentStrike()
        {
            var session = new CombatSession(0);
            using (var encounter = new CombatEncounter(session, new InteractionPhase(3, InteractionPhaseKind.EnemySequence)))
            {
                encounter.CommitStrike(Strike(), 0);
                for (long t = 100000; t <= 600000; t += 100000)
                    session.AdvanceFrame(t, new GestureCommand[0]);
                session.AdvanceFrame(650000, new[] { Parry(1, 650000, 1) });
                Assert.That(encounter.Player.Health, Is.EqualTo(75));
            }
        }

        [Test]
        public void ResumeParryUsesRetimedStrikeAndDoesNotRetainHeldGuard()
        {
            var session = new CombatSession(0);
            using (var encounter = new CombatEncounter(session, Phase))
            {
                var results = new List<DefenseResolution>();
                encounter.StrikeResolved += results.Add;
                encounter.CommitStrike(Strike(), 0);
                session.AdvanceInputFrame(100000, new[] { new CombatInput(
                    new DefenseCommand(1, 50000, DefenseCommandKind.GuardPress)) });
                for (long t = 200000; t <= 600000; t += 100000)
                    session.AdvanceFrame(t, new GestureCommand[0]);
                session.Suspend(610000, CombatSuspensionReason.FocusLost);
                Assert.That(encounter.Player.GuardHeld, Is.False);
                session.BeginResume(610000, 100000);
                session.AdvanceFrame(710000, new GestureCommand[0]);
                Assert.That(session.Timeline.TryGetNextImpact(out var impact), Is.True);
                Assert.That(impact.TimeUs, Is.EqualTo(1250000));
                for (long t = 810000; t <= 1210000; t += 100000)
                    session.AdvanceFrame(t, new GestureCommand[0]);
                session.AdvanceFrame(1260000, new[] { Parry(2, 1220000) });
                session.AdvanceFrame(1360000, new GestureCommand[0]);
                Assert.That(results.Single().Outcome, Is.EqualTo(DefenseOutcome.Parried));
                Assert.That(encounter.Player.Guard, Is.EqualTo(100));
            }
        }

        [Test]
        public void DeathCannotBeReversedByTiedRecoveryOrLaterInput()
        {
            var session = new CombatSession(0);
            using (var encounter = new CombatEncounter(session, Phase))
            {
                var lethal = new EnemyStrike(1, DefenseMask.Parry, DodgeSide.None,
                    SwipeDirection.Right, 0, 100, 650000, 0);
                encounter.CommitStrike(lethal, 0);
                for (long t = 100000; t <= 600000; t += 100000)
                    session.AdvanceFrame(t, new GestureCommand[0]);
                session.AdvanceFrame(650000, new GestureCommand[0]);
                session.AdvanceInputFrame(700000, new[] { new CombatInput(
                    new DefenseCommand(1, 690000, DefenseCommandKind.GuardPress)) });
                Assert.That(encounter.Player.Health, Is.Zero);
                Assert.That(encounter.Player.State, Is.EqualTo(PlayerCombatState.Dead));
                Assert.That(encounter.Player.GuardHeld, Is.False);
            }
        }
    }
}
