using System;
using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Application.Combat;
using Praxen.Game.Application.Input;
using Praxen.Game.Content.Combat;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class EnemyPatternDirectorTests
    {
        [Test]
        public void PrototypeIntroCommitsTheCommonTeachingStrikeAndCapturedRevisions()
        {
            var deck = PrototypeEnemyDecks.Create(EnemyArchetype.Sword);
            using var rig = new Rig(deck);
            Assert.That(rig.Director.Start(), Is.True);
            Assert.That(rig.Director.State, Is.EqualTo(EnemyDirectorState.Executing));
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
            Assert.That(rig.Director.CurrentDecision.Pattern.Id, Is.EqualTo(deck.IntroPatternId));
            var scheduled = rig.Director.CurrentSchedule[0];
            Assert.That(scheduled.Strike.Id, Is.EqualTo(1));
            Assert.That(scheduled.TelegraphTimeUs, Is.EqualTo(10000));
            Assert.That(scheduled.ImpactTimeUs, Is.EqualTo(660000));
            Assert.That(scheduled.RecoveryTimeUs, Is.EqualTo(1160000));
            Assert.That(scheduled.Strike.RequiredParryDirection, Is.EqualTo(SwipeDirection.Up));
            Assert.That(scheduled.Strike.SafeDodgeSides, Is.EqualTo(DodgeSide.Left));
            Assert.That(rig.Director.ContentRevision, Is.EqualTo(deck.ContentRevision));
            Assert.That(rig.Director.BalanceRevision, Is.EqualTo(deck.BalanceRevision));
        }

        [Test]
        public void CapacityFailureKeepsPreparedChoiceAndRetriesWithFutureTelegraph()
        {
            using var rig = new Rig(Deck(Pattern("only")), capacity: 4);
            rig.Session.Timeline.TrySchedule(new CombatMilestone(900, 100000,
                CombatMilestoneKind.RecoveryComplete));
            uint randomBefore = rig.Director.Selector.RandomState;
            Assert.That(rig.Director.Start(), Is.False);
            var prepared = rig.Director.CurrentDecision;
            Assert.That(rig.Director.State, Is.EqualTo(EnemyDirectorState.Prepared));
            Assert.That(rig.Director.Selector.SelectionCount, Is.Zero);
            Assert.That(rig.Director.Selector.RandomState, Is.EqualTo(randomBefore));
            Assert.That(rig.Director.Selector.History, Is.Empty);
            Assert.That(rig.Session.Timeline.PendingCount, Is.EqualTo(1));
            rig.AdvanceTo(100000);
            Assert.That(rig.Director.CurrentDecision, Is.SameAs(prepared));
            Assert.That(rig.Director.State, Is.EqualTo(EnemyDirectorState.Executing));
            Assert.That(rig.Director.CurrentSchedule[0].TelegraphTimeUs, Is.EqualTo(110000));
            Assert.That(rig.Director.Selector.History, Is.EqualTo(new[] { "only" }));
        }

        [Test]
        public void ForeignStrikeIdentityCollisionLeavesTheChoiceUncommittedUntilRetry()
        {
            using var rig = new Rig(Deck(Pattern("only")));
            rig.Session.Timeline.TrySchedule(new CombatMilestone(1, 100000,
                CombatMilestoneKind.RecoveryComplete));
            Assert.That(rig.Director.Start(), Is.False);
            var prepared = rig.Director.CurrentDecision;
            Assert.That(rig.Encounter.HasCommittedStrike, Is.False);
            Assert.That(rig.Director.Selector.SelectionCount, Is.Zero);
            rig.AdvanceTo(100000);
            Assert.That(rig.Director.CurrentDecision, Is.SameAs(prepared));
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
            Assert.That(rig.Director.CurrentSchedule[0].Strike.Id, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedSequenceReadyCallbacksRetryTheSamePreparedChoiceAfterQueuePressure()
        {
            using var rig = new Rig(Deck(Pattern("a"), Pattern("b"), Pattern("c")),
                seed: 1234567, capacity: 4);
            var decisions = new List<EnemyPatternDecision>();
            rig.Duel.EnemySequenceReady += time => decisions.Add(rig.Director.CurrentDecision);
            rig.Director.Start();
            rig.AdvanceTo(3100000);
            for (int index = 1; index <= 3; index++)
                rig.Session.Timeline.TrySchedule(new CombatMilestone(900 + index, 3160000 + index,
                    CombatMilestoneKind.RecoveryComplete));
            rig.AdvanceFrame(3200000);
            Assert.That(decisions, Has.Count.EqualTo(2));
            Assert.That(decisions[1], Is.SameAs(decisions[0]));
            Assert.That(rig.Director.CurrentDecision.Pattern.Id, Is.EqualTo("c"));
            Assert.That(rig.Director.CurrentDecision.PreparedAtUs, Is.EqualTo(3160000));
            Assert.That(rig.Director.CurrentSchedule[0].TelegraphTimeUs, Is.EqualTo(3210000));
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(2));
            Assert.That(rig.Director.Selector.RandomState, Is.EqualTo(42034982));
        }

        [Test]
        public void WholeTwoStepPatternHasNoOverlapAndOnlyOneFinalOpening()
        {
            var pattern = new EnemyPattern("pair", new[] { Step("one"), Step("two", 40000) });
            using var rig = new Rig(Deck(pattern));
            var ready = new List<long>();
            rig.Duel.EnemySequenceReady += ready.Add;
            Assert.That(rig.Director.Start(), Is.True);
            Assert.That(rig.Session.Timeline.PendingCount, Is.EqualTo(7));
            Assert.That(rig.Director.CurrentSchedule[1].TelegraphTimeUs, Is.EqualTo(1200000));
            Assert.That(rig.Director.CurrentSchedule[1].RecoveryTimeUs, Is.EqualTo(2350000));
            rig.AdvanceTo(1160000);
            Assert.That(rig.Director.State, Is.EqualTo(EnemyDirectorState.Executing));
            Assert.That(rig.Duel.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.EnemySequence));
            rig.AdvanceTo(2350000);
            Assert.That(rig.Director.State, Is.EqualTo(EnemyDirectorState.PlayerOpening));
            Assert.That(rig.Duel.CurrentPhase.Id, Is.EqualTo(2));
            Assert.That(ready, Is.Empty);
            rig.AdvanceTo(4350000);
            Assert.That(ready, Is.EqualTo(new long[] { 4350000 }));
            Assert.That(rig.Duel.CurrentPhase.Id, Is.EqualTo(3));
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(2));
        }

        [Test]
        public void NextSequenceUsesExactOpeningBoundaryRatherThanTheFrameHorizon()
        {
            using var rig = new Rig(Deck(Pattern("only")));
            rig.Director.Start();
            rig.AdvanceTo(3100000);
            rig.AdvanceFrame(3200000);
            Assert.That(rig.Session.Clock.TimeUs, Is.EqualTo(3200000));
            Assert.That(rig.Director.CurrentDecision.PreparedAtUs, Is.EqualTo(3160000));
            Assert.That(rig.Director.CurrentSchedule[0].TelegraphTimeUs, Is.EqualTo(3170000));
            Assert.That(rig.Director.CurrentSchedule[0].Strike.Id, Is.EqualTo(2));
        }

        [Test]
        public void CooldownWakesAtItsExactAdmissionDeadlineAndUsesTheCallbackTime()
        {
            using var rig = new Rig(Deck(Pattern("only", cooldownUs: 4000000)));
            rig.Director.Start();
            rig.AdvanceTo(3160000);
            Assert.That(rig.Director.State, Is.EqualTo(EnemyDirectorState.Waiting));
            Assert.That(rig.Session.Timeline.PendingCount, Is.EqualTo(1));
            rig.AdvanceTo(3999999);
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
            rig.AdvanceFrame(4050000);
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(2));
            Assert.That(rig.Director.CurrentDecision.PreparedAtUs, Is.EqualTo(4000000));
            Assert.That(rig.Director.CurrentSchedule[0].TelegraphTimeUs, Is.EqualTo(4010000));
        }

        [Test]
        public void CooldownStartsOnSuccessfulAdmissionAfterQueuePressure()
        {
            using var rig = new Rig(Deck(Pattern("only", cooldownUs: 4000000)), capacity: 4);
            rig.Session.Timeline.TrySchedule(new CombatMilestone(900, 100000,
                CombatMilestoneKind.RecoveryComplete));
            rig.Director.Start();
            rig.AdvanceTo(100000);
            rig.AdvanceTo(4000000);
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
            rig.AdvanceTo(4099999);
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
            rig.AdvanceTo(4100000);
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(2));
            Assert.That(rig.Director.CurrentSchedule[0].TelegraphTimeUs, Is.EqualTo(4110000));
        }

        [Test]
        public void SuspendFreezesWaitingCooldownAndPreservesSelectionHistory()
        {
            using var rig = new Rig(Deck(Pattern("only", cooldownUs: 4000000)));
            rig.Director.Start();
            rig.AdvanceTo(3200000);
            rig.Session.Suspend(rig.DeviceTimeUs, CombatSuspensionReason.ApplicationPaused);
            rig.AdvanceDeviceTo(rig.DeviceTimeUs + 1000000);
            Assert.That(rig.Session.Clock.TimeUs, Is.EqualTo(3200000));
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
            Assert.That(rig.Director.Selector.History, Is.EqualTo(new[] { "only" }));
            rig.Resume();
            rig.AdvanceTo(3999999);
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
            rig.AdvanceTo(4000000);
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(2));
        }

        [Test]
        public void SuspendKeepsPreparedDecisionAndResumesItsCapacityRetry()
        {
            using var rig = new Rig(Deck(Pattern("only")), capacity: 4);
            rig.Session.Timeline.TrySchedule(new CombatMilestone(900, 100000,
                CombatMilestoneKind.RecoveryComplete));
            rig.Director.Start();
            var prepared = rig.Director.CurrentDecision;
            rig.Session.Suspend(0, CombatSuspensionReason.FocusLost);
            rig.AdvanceDeviceTo(300000);
            Assert.That(rig.Director.CurrentDecision, Is.SameAs(prepared));
            Assert.That(rig.Director.Selector.SelectionCount, Is.Zero);
            rig.Resume();
            rig.AdvanceTo(100000);
            Assert.That(rig.Director.CurrentDecision, Is.SameAs(prepared));
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
        }

        [Test]
        public void PreparedChoiceCannotReactToUnresolvedDefenseOrLaterResources()
        {
            var intro = Pattern("intro");
            var focused = new EnemyPattern("focused", new[] { Step("focused") }, minimumFocus: 25);
            using var rig = new Rig(Deck(intro, focused));
            rig.Director.Start();
            var committed = rig.Director.CurrentDecision;
            rig.AdvanceTo(650000, Parry(1, 650000, 1));
            Assert.That(rig.Duel.Momentum.Focus, Is.Zero);
            Assert.That(rig.Director.CurrentDecision, Is.SameAs(committed));
            rig.AdvanceTo(660000);
            Assert.That(rig.Duel.Momentum.Focus, Is.EqualTo(25));
            Assert.That(rig.Director.CurrentDecision, Is.SameAs(committed));
            rig.AdvanceTo(3160000);
            Assert.That(rig.Director.CurrentDecision.Pattern.Id, Is.EqualTo("focused"));
        }

        [Test]
        public void ResourceIneligibleDeckWaitsAndCanRetryOnApprovedCompletedDefense()
        {
            var focused = new EnemyPattern("focused", new[] { Step("focused") }, minimumFocus: 25);
            using var rig = new Rig(Deck(focused));
            Assert.That(rig.Director.Start(), Is.False);
            Assert.That(rig.Director.State, Is.EqualTo(EnemyDirectorState.Waiting));
            Assert.That(rig.Director.Selector.SelectionCount, Is.Zero);
            Assert.That(rig.Duel.CommitStrike(new EnemyStrike(20, DefenseMask.Parry,
                DodgeSide.None, SwipeDirection.Up, 0, 0), 10000), Is.True);
            rig.AdvanceTo(650000, Parry(1, 650000, 1));
            rig.AdvanceTo(3160000);
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
            Assert.That(rig.Director.CurrentDecision.Pattern.Id, Is.EqualTo("focused"));
        }

        [Test]
        public void SeedRestartProducesTheSamePatternTraceWithFreshHistory()
        {
            var deck = Deck(Pattern("a"), Pattern("b"), Pattern("c"));
            var first = Trace(deck, 1234567);
            var restarted = Trace(deck, 1234567);
            Assert.That(first, Has.Count.EqualTo(6));
            Assert.That(restarted, Is.EqualTo(first));
            Assert.That(first[0], Is.EqualTo("a"));
            for (int index = 1; index < first.Count; index++)
                Assert.That(first[index], Is.Not.EqualTo(first[index - 1]));
        }

        [Test]
        public void CurrentScheduleIsReadOnlyAndUnaffectedByLaterCommits()
        {
            using var rig = new Rig(Deck(Pattern("only")));
            rig.Director.Start();
            var first = rig.Director.CurrentSchedule;
            Assert.Throws<NotSupportedException>(() => ((IList<ScheduledEnemyStrike>)first).Clear());
            rig.AdvanceTo(3160000);
            Assert.That(first[0].Strike.Id, Is.EqualTo(1));
            Assert.That(first[0].TelegraphTimeUs, Is.EqualTo(10000));
            Assert.That(rig.Director.CurrentSchedule[0].Strike.Id, Is.EqualTo(2));
        }

        [Test]
        public void DisposeCancelsOnlyOwnedCooldownWakeAndUnsubscribesRetries()
        {
            using var rig = new Rig(Deck(Pattern("only", cooldownUs: 4000000)));
            rig.Director.Start();
            rig.AdvanceTo(3160000);
            rig.Session.Timeline.TrySchedule(new CombatMilestone(900, 4500000,
                CombatMilestoneKind.PhaseTransition));
            rig.Director.Dispose();
            Assert.That(rig.Director.State, Is.EqualTo(EnemyDirectorState.Terminal));
            Assert.That(rig.Session.Timeline.PendingCount, Is.EqualTo(1));
            Assert.That(rig.Session.Timeline.TryGetMilestone(900, out _), Is.True);
            rig.AdvanceTo(4500000);
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolvedWakeIdentityCanBeReusedWithoutDirectorCancelingForeignRecord()
        {
            using var rig = new Rig(Deck(Pattern("only", cooldownUs: 4000000)));
            rig.Director.Start();
            rig.AdvanceTo(3160000);
            long reusedId = 0;
            rig.Session.Timeline.Resolved += record =>
            {
                if (record.TimeUs != 4000000 || !record.Milestone.HasValue) return;
                reusedId = record.Milestone.Value.Id;
                Assert.That(rig.Session.Timeline.TrySchedule(new CombatMilestone(reusedId,
                    4500000, CombatMilestoneKind.PhaseTransition)), Is.True);
            };
            rig.AdvanceTo(4000000);
            Assert.That(reusedId, Is.GreaterThan(0));
            rig.Director.Dispose();
            Assert.That(rig.Session.Timeline.TryGetMilestone(reusedId, out _), Is.True);
        }

        [Test]
        public void CanceledWakeCanBeReplacedWithExactForeignValuesAndSurviveDispose()
        {
            using var rig = new Rig(Deck(Pattern("only", cooldownUs: 4000000)));
            rig.Director.Start();
            rig.AdvanceTo(3160000);
            long id = FindWake(rig.Session.Timeline, 4000000);
            Assert.That(rig.Session.Timeline.TryCancelMilestone(id), Is.True);
            Assert.That(rig.Session.Timeline.TrySchedule(new CombatMilestone(id,
                4000000, CombatMilestoneKind.PhaseTransition)), Is.True);
            rig.Director.Dispose();
            Assert.That(rig.Session.Timeline.TryGetMilestone(id, out _), Is.True);
        }

        [Test]
        public void TerminalDefeatStopsSelectionAndPreservesForeignMilestone()
        {
            var fatal = new EnemyPattern("fatal", new[] { new EnemyPatternStep("fatal",
                DefenseMask.Parry, DodgeSide.None, SwipeDirection.Up, 0, 100) });
            using var rig = new Rig(Deck(fatal));
            rig.Session.Timeline.TrySchedule(new CombatMilestone(900, 2000000,
                CombatMilestoneKind.PhaseTransition));
            rig.Director.Start();
            rig.AdvanceTo(660000);
            Assert.That(rig.Duel.Outcome, Is.EqualTo(DuelOutcome.Defeat));
            Assert.That(rig.Director.State, Is.EqualTo(EnemyDirectorState.Terminal));
            Assert.That(rig.Session.Timeline.TryGetMilestone(900, out _), Is.True);
            rig.AdvanceTo(2000000);
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
            Assert.That(rig.Director.Start(), Is.False);
        }

        [Test]
        public void PausedStartupArmsOneAdmissionAndUnsafePhasesRejectFurtherStarts()
        {
            using var rig = new Rig(Deck(Pattern("only")));
            rig.Session.Suspend(0, CombatSuspensionReason.FocusLost);
            Assert.That(rig.Director.Start(), Is.False);
            Assert.That(rig.Director.CurrentDecision, Is.Null);
            rig.Resume();
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
            Assert.That(rig.Director.State, Is.EqualTo(EnemyDirectorState.Executing));
            Assert.That(rig.Director.Start(), Is.False);
            rig.AdvanceTo(1160000);
            Assert.That(rig.Director.Start(), Is.False);
            Assert.That(rig.Director.Selector.SelectionCount, Is.EqualTo(1));
        }

        private static EnemyPatternStep Step(string id, long gapBeforeUs = 0) => new EnemyPatternStep(
            id, DefenseMask.Parry | DefenseMask.Dodge, DodgeSide.Left, SwipeDirection.Up,
            0, 0, gapBeforeUs: gapBeforeUs);

        private static long FindWake(CombatTimeline timeline, long timeUs)
        {
            for (long id = long.MaxValue; id > long.MaxValue - 16; id--)
                if (timeline.TryGetMilestone(id, out var milestone) && milestone.TimeUs == timeUs &&
                    milestone.Kind == CombatMilestoneKind.PhaseTransition) return id;
            throw new AssertionException("Expected the director's pending cooldown wake.");
        }

        private static EnemyPattern Pattern(string id, long cooldownUs = 0) =>
            new EnemyPattern(id, new[] { Step(id) }, cooldownUs: cooldownUs);

        private static EnemyPatternDeck Deck(params EnemyPattern[] patterns) => new EnemyPatternDeck(
            "test", EnemyArchetype.Sword, "Test timing", "content-test", "balance-test",
            patterns, patterns[0].Id);

        private static GestureCommand Parry(long sequence, long timestamp, long phaseId) =>
            new GestureCommand(sequence, timestamp, 1, SwipeDirection.Up, GestureIntent.Parry,
                new InteractionPhase(phaseId, InteractionPhaseKind.EnemySequence),
                new NormalizedPoint(0.5, 0.5), new NormalizedPoint(0.5, 0.8));

        private static List<string> Trace(EnemyPatternDeck deck, uint seed)
        {
            using var rig = new Rig(deck, seed);
            var trace = new List<string>();
            rig.Director.PatternCommitted += decision => trace.Add(decision.Pattern.Id);
            rig.Director.Start();
            while (trace.Count < 6) rig.AdvanceTo(rig.Session.Clock.TimeUs + 100000);
            return trace;
        }

        private sealed class Rig : IDisposable
        {
            internal readonly CombatSession Session;
            internal readonly CombatEncounter Encounter;
            internal readonly CombatOpeningController Duel;
            internal readonly EnemyPatternDirector Director;
            internal long DeviceTimeUs => Session.Clock.LastDeviceTimeUs;

            internal Rig(EnemyPatternDeck deck, uint seed = 123, int capacity = 1024)
            {
                Session = new CombatSession(0, capacity);
                var phase = new InteractionPhase(1, InteractionPhaseKind.EnemySequence);
                Encounter = new CombatEncounter(Session, phase);
                Duel = new CombatOpeningController(Encounter, new GesturePhaseContext());
                Director = new EnemyPatternDirector(Duel, deck, seed);
            }

            internal void AdvanceTo(long target, params GestureCommand[] commands)
            {
                while (Session.Clock.TimeUs < target)
                {
                    long next = Math.Min(target, Session.Clock.TimeUs + 100000);
                    Session.AdvanceFrame(DeviceTimeUs + next - Session.Clock.TimeUs,
                        next == target ? commands : Array.Empty<GestureCommand>());
                }
            }

            internal void AdvanceFrame(long combatTimeUs) => Session.AdvanceFrame(
                DeviceTimeUs + combatTimeUs - Session.Clock.TimeUs, Array.Empty<GestureCommand>());

            internal void AdvanceDeviceTo(long target)
            {
                while (DeviceTimeUs < target)
                    Session.AdvanceFrame(Math.Min(target, DeviceTimeUs + 100000),
                        Array.Empty<GestureCommand>());
            }

            internal void Resume()
            {
                Session.BeginResume(DeviceTimeUs, 100000);
                AdvanceDeviceTo(DeviceTimeUs + 100000);
            }

            public void Dispose()
            {
                Director.Dispose();
                Duel.Dispose();
                Encounter.Dispose();
            }
        }
    }
}
