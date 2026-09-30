using System;
using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class OffenseTimingSeamTests
    {
        [Test]
        public void BeforeResolveObservesSealedEventBeforeConsumers()
        {
            var timeline = new CombatTimeline();
            var order = new List<string>();
            timeline.BeforeResolve += record =>
            {
                Assert.That(timeline.TimeUs, Is.EqualTo(100));
                Assert.That(timeline.TrySchedule(new CombatMilestone(2, 100,
                    CombatMilestoneKind.PhaseTransition)), Is.False);
                order.Add("before");
            };
            timeline.Resolved += record => order.Add("resolved");
            timeline.TrySchedule(new CombatMilestone(1, 100, CombatMilestoneKind.PlayerImpact));
            timeline.AdvanceTo(100);
            Assert.That(order, Is.EqualTo(new[] { "before", "resolved" }));
        }

        [Test]
        public void PlayerImpactHasImpactPriorityButIsNeverAnIncomingWarning()
        {
            var timeline = new CombatTimeline();
            var kinds = new List<CombatMilestoneKind>();
            timeline.Resolved += record => kinds.Add(record.Milestone.Value.Kind);
            timeline.TrySchedule(new CombatMilestone(1, 100, CombatMilestoneKind.PhaseTransition));
            timeline.TrySchedule(new CombatMilestone(2, 100, CombatMilestoneKind.PlayerImpact));
            Assert.That(timeline.TryGetNextImpact(out _), Is.False);
            Assert.That(timeline.EnsureIncomingWarning(), Is.EqualTo(-1));
            timeline.AdvanceTo(100);
            Assert.That(kinds, Is.EqualTo(new[] { CombatMilestoneKind.PlayerImpact,
                CombatMilestoneKind.PhaseTransition }));
        }

        [Test]
        public void CurrentMilestoneIdStaysReservedThroughPreResolutionOwnershipHandoff()
        {
            var timeline = new CombatTimeline();
            timeline.TrySchedule(new CombatMilestone(1, 100, CombatMilestoneKind.PhaseTransition));
            timeline.BeforeResolve += record =>
            {
                if (record.TimeUs == 100)
                    Assert.That(timeline.TrySchedule(new CombatMilestone(1, 101,
                        CombatMilestoneKind.Telegraph)), Is.False);
            };
            timeline.Resolved += record =>
            {
                if (record.TimeUs == 100)
                    Assert.That(timeline.TrySchedule(new CombatMilestone(1, 101,
                        CombatMilestoneKind.Telegraph)), Is.True);
            };
            timeline.AdvanceTo(101);
            Assert.That(timeline.PendingCount, Is.Zero);
        }

        [Test]
        public void GeneratedIdsAvoidExistingIdsAndLookupReflectsResumeShift()
        {
            var timeline = new CombatTimeline(3);
            timeline.TrySchedule(new CombatMilestone(long.MaxValue, 100, CombatMilestoneKind.Impact));
            var id = timeline.AllocateMilestoneId();
            Assert.That(id, Is.EqualTo(long.MaxValue - 1));
            timeline.TrySchedule(new CombatMilestone(id, 200, CombatMilestoneKind.PhaseTransition));
            Assert.That(timeline.AvailableCapacity, Is.EqualTo(1));
            timeline.EnsureIncomingWarning();
            Assert.That(timeline.TryGetMilestone(id, out var marker), Is.True);
            Assert.That(marker.TimeUs, Is.EqualTo(650100));
            Assert.That(timeline.TryCancelMilestone(id), Is.True);
            Assert.That(timeline.TryGetMilestone(id, out _), Is.False);
        }

        [Test]
        public void OrderedTouchPreservesSourceAndExclusivePayload()
        {
            var sample = new TouchSample(7, SamplePhase.Began, new NormalizedPoint(.5, .5), 123);
            var touch = new OrderedTouchRecord(sample, PointerOwnership.Gameplay, new ScreenMetrics(720, 1280));
            var input = new CombatInput(touch);
            Assert.That(input.IsValid, Is.True);
            Assert.That(input.SourceTimestampUs, Is.EqualTo(123));
            Assert.That(input.Gesture.HasValue || input.Defense.HasValue, Is.False);
            var timeline = new CombatTimeline();
            CombatTimelineEvent resolved = default;
            timeline.Resolved += record => resolved = record;
            Assert.That(timeline.TryEnqueue(touch, 50), Is.True);
            timeline.AdvanceTo(50);
            Assert.That(resolved.Touch.Value.Sample.TimestampUs, Is.EqualTo(123));
            Assert.That(resolved.TimeUs, Is.EqualTo(50));
            Assert.That(resolved.Command.HasValue || resolved.DefenseCommand.HasValue ||
                resolved.Milestone.HasValue, Is.False);
        }

        [Test]
        public void SharedPlayerActionLocksDefenseThroughAttackAndRecovery()
        {
            var player = new DefenseCombatant();
            Assert.That(player.TryBeginOffense(10, 110, 510), Is.True);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Attacking));
            Assert.That(player.ApplyControl(new DefenseCommand(1, 20, DefenseCommandKind.DodgeLeft), 20), Is.False);
            player.AdvanceTo(110);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Recovery));
            player.ReleaseHeldDefense();
            player.AdvanceTo(509);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Recovery));
            player.AdvanceTo(510);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
            Assert.That(player.DodgeCharges, Is.EqualTo(3));
        }

        [Test]
        public void MalformedActionTimesDoNotChangePlayer()
        {
            var player = new DefenseCombatant();
            Assert.Throws<ArgumentOutOfRangeException>(() => player.TryBeginOffense(10, 10, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => player.TryBeginOffense(10, 20, 20));
            Assert.That(player.TimeUs, Is.Zero);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
        }
    }
}
