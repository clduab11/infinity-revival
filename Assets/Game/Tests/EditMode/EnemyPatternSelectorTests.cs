using System;
using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class EnemyPatternSelectorTests
    {
        [Test]
        public void NewSelectorOwnsItsDeckAndStartsWithoutSelectionHistory()
        {
            var deck = Deck(Pattern("a"));
            var selector = new EnemyPatternSelector(deck, 42, 5);

            Assert.That(selector.Deck, Is.SameAs(deck));
            Assert.That(selector.InitialSeed, Is.EqualTo(42u));
            Assert.That(selector.RandomState, Is.EqualTo(42u));
            Assert.That(selector.SelectionCount, Is.Zero);
            Assert.That(selector.History, Is.Empty);
        }

        [TestCase(-1)]
        [TestCase(6)]
        public void InvalidDifficultyTierRejectsConstruction(int tier)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EnemyPatternSelector(Deck(Pattern("a")), 1, tier));
        }

        [Test]
        public void MissingDeckRejectsConstruction()
        {
            Assert.Throws<ArgumentNullException>(() => new EnemyPatternSelector(null, 1));
        }

        [Test]
        public void ZeroSeedNormalizesToTheAuthoredNonzeroState()
        {
            var selector = new EnemyPatternSelector(WeightedDeck(), 0);
            string[] expected = { "a", "c", "b", "c", "b", "c", "a", "c", "a", "c" };
            uint[] states = { 1085196063u, 2447379481u, 2618286376u, 1701901981u,
                265159372u, 1030440423u, 4012273292u, 2080899351u, 4184948546u, 230363598u };

            Assert.That(selector.InitialSeed, Is.EqualTo(0x6D2B79F5u));
            AssertLiteralSequence(selector, expected, states);
        }

        [Test]
        public void SeededWeightsAndTwoEntryHistoryProduceAnIndependentLiteralSequence()
        {
            var selector = new EnemyPatternSelector(WeightedDeck(), 1);
            string[] expected = { "c", "b", "c", "a", "c", "a", "b", "c", "a", "b" };
            uint[] states = { 270369u, 67634689u, 2647435461u, 307599695u, 2398689233u,
                745495504u, 632435482u, 435756210u, 2005365029u, 2916098932u };

            AssertLiteralSequence(selector, expected, states);
        }

        [TestCase(4071982377u, "a", 1u)]
        [TestCase(3848997458u, "b", 2u)]
        [TestCase(400461691u, "a", 3u)]
        public void BoundedDrawTreatsTheFirstNonzeroRngValueAsSampleZero(
            uint seed, string expected, uint after)
        {
            var selector = new EnemyPatternSelector(Deck(Pattern("a", 1), Pattern("b", 1)), seed);

            var decision = Prepare(selector, 0);

            Assert.That(decision.Pattern.Id, Is.EqualTo(expected));
            Assert.That(decision.RandomStateBefore, Is.EqualTo(seed));
            Assert.That(decision.RandomStateAfter, Is.EqualTo(after));
            Assert.That(selector.RandomState, Is.EqualTo(seed));
        }

        [Test]
        public void UnbiasedDrawRejectsTheUnpairedUpperValueInsteadOfTakingModulo()
        {
            // This seed yields 4294967295, then 253983. The first sample cannot pair
            // evenly across two candidates in xorshift32's 4294967295-value domain.
            var selector = new EnemyPatternSelector(Deck(Pattern("a", 1), Pattern("b", 1)), 1584200935u);

            var decision = Prepare(selector, 0);

            Assert.That(decision.Pattern.Id, Is.EqualTo("a"));
            Assert.That(decision.RandomStateAfter, Is.EqualTo(253983u));
        }

        [Test]
        public void PrepareNeverMutatesRandomStateHistoryOrCooldowns()
        {
            var selector = new EnemyPatternSelector(Deck(Pattern("a", cooldownUs: 100)), 1);
            var first = Prepare(selector, 100);
            var second = Prepare(selector, 200);

            Assert.That(first.Pattern, Is.SameAs(second.Pattern));
            Assert.That(first.RandomStateAfter, Is.EqualTo(second.RandomStateAfter));
            Assert.That(first.PreparedAtUs, Is.EqualTo(100));
            Assert.That(second.PreparedAtUs, Is.EqualTo(200));
            Assert.That(first.SelectionIndex, Is.Zero);
            Assert.That(selector.RandomState, Is.EqualTo(1u));
            Assert.That(selector.SelectionCount, Is.Zero);
            Assert.That(selector.History, Is.Empty);
            Assert.That(selector.CanCommit(first, 100), Is.True);
            Assert.That(selector.CanCommit(second, 200), Is.True);
        }

        [Test]
        public void AdmissionRetryCommitsThePreparedChoiceAndStartsCooldownAtAdmission()
        {
            var selector = new EnemyPatternSelector(Deck(Pattern("a", cooldownUs: 4000)), 1);
            var decision = Prepare(selector, 100);

            Assert.That(selector.CanCommit(decision, 2000), Is.True);
            Assert.That(selector.TryCommit(decision, 2000), Is.True);
            Assert.That(selector.RandomState, Is.EqualTo(decision.RandomStateAfter));
            Assert.That(selector.SelectionCount, Is.EqualTo(1));
            Assert.That(selector.History, Is.EqualTo(new[] { "a" }));
            Assert.That(selector.TryPrepare(5999, Observed(), out var early, out var deadline), Is.False);
            Assert.That(early, Is.Null);
            Assert.That(deadline, Is.EqualTo(6000));
            Assert.That(selector.TryPrepare(6000, Observed(), out var ready, out deadline), Is.True);
            Assert.That(ready.Pattern.Id, Is.EqualTo("a"));
            Assert.That(deadline, Is.Null);
        }

        [Test]
        public void ConfiguredFirstTeachingPatternBypassesTheDraw()
        {
            var deck = DeckWithIntro("teach", Pattern("other", 1000000), Pattern("teach", 1));
            var selector = new EnemyPatternSelector(deck, 1);
            var teaching = Prepare(selector, 10);

            Assert.That(teaching.Pattern.Id, Is.EqualTo("teach"));
            Assert.That(teaching.RandomStateBefore, Is.EqualTo(1u));
            Assert.That(teaching.RandomStateAfter, Is.EqualTo(1u));
            Assert.That(selector.TryCommit(teaching, 10), Is.True);
            var subsequent = Prepare(selector, 10);
            Assert.That(subsequent.Pattern.Id, Is.EqualTo("other"));
            Assert.That(subsequent.RandomStateAfter, Is.EqualTo(270369u));
        }

        [Test]
        public void IneligibleIntroAllowsTheFirstAdmissibleFallback()
        {
            var deck = DeckWithIntro("teach", Pattern("teach", minimumGuard: 10), Pattern("fallback"));
            var selector = new EnemyPatternSelector(deck, 1);
            var fallback = Prepare(selector, 0, Observed(guard: 0));

            Assert.That(fallback.Pattern.Id, Is.EqualTo("fallback"));
            Assert.That(selector.TryCommit(fallback, 0), Is.True);
            var following = Prepare(selector, 1, Observed(guard: 10));
            Assert.That(following.RandomStateAfter, Is.EqualTo(67634689u));
        }

        [Test]
        public void FirstTeachingChoiceRemainsAvailableAfterARejectedAdmission()
        {
            var selector = new EnemyPatternSelector(DeckWithIntro("teach", Pattern("teach")), 1);
            var decision = Prepare(selector, 10);

            Assert.That(selector.TryCommit(decision, 9), Is.False);
            Assert.That(Prepare(selector, 20).Pattern.Id, Is.EqualTo("teach"));
            Assert.That(selector.RandomState, Is.EqualTo(1u));
            Assert.That(selector.History, Is.Empty);
        }

        [Test]
        public void ImmediateRepeatIsSuppressedEvenWithDominantAuthoredWeight()
        {
            var selector = new EnemyPatternSelector(Deck(Pattern("a", 1000000), Pattern("b", 1)), 1);

            Assert.That(CommitNext(selector, 0).Pattern.Id, Is.EqualTo("a"));
            Assert.That(Prepare(selector, 1).Pattern.Id, Is.EqualTo("b"));
        }

        [Test]
        public void OnlyOffCooldownCandidateMayRepeat()
        {
            var selector = new EnemyPatternSelector(Deck(Pattern("a", 1, 100), Pattern("b", 1)), 4071982377u);

            Assert.That(CommitNext(selector, 0).Pattern.Id, Is.EqualTo("a"));
            Assert.That(CommitNext(selector, 1).Pattern.Id, Is.EqualTo("b"));
            Assert.That(Prepare(selector, 2).Pattern.Id, Is.EqualTo("b"));
        }

        [Test]
        public void RecentWeightHalvingKeepsWeightOneSelectable()
        {
            var selector = new EnemyPatternSelector(Deck(Pattern("a", 1), Pattern("b", 1), Pattern("c", 1)), 1);
            string[] expected = { "c", "a", "b", "a", "b", "c", "b", "c", "a", "c" };
            uint[] states = { 270369u, 67634689u, 2647435461u, 307599695u, 2398689233u,
                745495504u, 632435482u, 435756210u, 2005365029u, 2916098932u };

            AssertLiteralSequence(selector, expected, states);
        }

        [Test]
        public void HistorySnapshotsRetainOnlyTheLastTwoCommittedIdsAndRejectWrites()
        {
            var selector = new EnemyPatternSelector(WeightedDeck(), 1);
            CommitNext(selector, 0);
            var firstSnapshot = selector.History;
            CommitNext(selector, 1);
            CommitNext(selector, 2);

            Assert.That(firstSnapshot, Is.EqualTo(new[] { "c" }));
            Assert.That(selector.History, Is.EqualTo(new[] { "b", "c" }));
            var list = selector.History as IList<string>;
            Assert.That(list, Is.Not.Null);
            Assert.Throws<NotSupportedException>(() => list[0] = "tampered");
            Assert.That(selector.History, Is.EqualTo(new[] { "b", "c" }));
        }

        [TestCase(9, 2, 30, 2, false)]
        [TestCase(10, 1, 30, 2, false)]
        [TestCase(10, 2, 29, 2, false)]
        [TestCase(10, 2, 30, 1, false)]
        [TestCase(10, 2, 30, 4, false)]
        [TestCase(10, 2, 30, 2, true)]
        [TestCase(10, 2, 30, 3, true)]
        public void AllApprovedResourceAndDifficultyRequirementsApplyBeforeSelection(
            int guard, int dodgeCharges, int focus, int tier, bool expected)
        {
            var selector = new EnemyPatternSelector(Deck(Pattern("a", minimumGuard: 10,
                minimumDodgeCharges: 2, minimumFocus: 30, minimumTier: 2, maximumTier: 3)), 1, tier);

            Assert.That(selector.TryPrepare(0, Observed(guard, dodgeCharges, focus),
                out var decision, out var deadline), Is.EqualTo(expected));
            Assert.That(decision != null, Is.EqualTo(expected));
            Assert.That(deadline, Is.Null);
            Assert.That(selector.RandomState, Is.EqualTo(1u));
            Assert.That(selector.History, Is.Empty);
        }

        [TestCase(null, "b")]
        [TestCase(DefenseOutcome.Blocked, "b")]
        [TestCase(DefenseOutcome.Parried, "a")]
        public void PreferredCompletedOutcomeDoublesOnlyItsMatchingWeight(
            DefenseOutcome? outcome, string expected)
        {
            var selector = new EnemyPatternSelector(Deck(
                Pattern("a", 1, preferredDefenseOutcome: DefenseOutcome.Parried), Pattern("b", 1)), 3848997458u);

            Assert.That(Prepare(selector, 0, Observed(outcome: outcome)).Pattern.Id, Is.EqualTo(expected));
        }

        [Test]
        public void LastResolvedAttackDirectionDoesNotAlterTheAuthoredDefensePreference()
        {
            var deck = Deck(Pattern("a", 1, preferredDefenseOutcome: DefenseOutcome.Parried), Pattern("b", 1));
            var left = new EnemyPatternSelector(deck, 3848997458u);
            var right = new EnemyPatternSelector(deck, 3848997458u);

            var first = Prepare(left, 0, new EnemyObservation(100, 100, 2, 100,
                DefenseOutcome.Parried, SwipeDirection.Left));
            var second = Prepare(right, 0, new EnemyObservation(100, 100, 2, 100,
                DefenseOutcome.Parried, SwipeDirection.Right));

            Assert.That(first.Pattern.Id, Is.EqualTo(second.Pattern.Id));
            Assert.That(first.RandomStateAfter, Is.EqualTo(second.RandomStateAfter));
        }

        [Test]
        public void LaterResourceObservationDoesNotInvalidateAnAlreadyPreparedChoice()
        {
            var selector = new EnemyPatternSelector(Deck(Pattern("a", minimumFocus: 30)), 1);
            var committedChoice = Prepare(selector, 0, Observed(focus: 30));

            Assert.That(selector.TryPrepare(1, Observed(focus: 0), out var rejected, out var deadline), Is.False);
            Assert.That(rejected, Is.Null);
            Assert.That(deadline, Is.Null);
            Assert.That(selector.CanCommit(committedChoice, 2), Is.True);
            Assert.That(selector.TryCommit(committedChoice, 2), Is.True);
        }

        [Test]
        public void CooldownWakeIgnoresCandidatesIneligibleForCurrentResources()
        {
            var selector = new EnemyPatternSelector(DeckWithIntro("a",
                Pattern("a", cooldownUs: 10, minimumGuard: 10),
                Pattern("b", cooldownUs: 20, minimumFocus: 5)), 1);
            CommitNext(selector, 0);
            CommitNext(selector, 2);

            Assert.That(selector.TryPrepare(3, Observed(guard: 0, focus: 5), out var decision,
                out var deadline), Is.False);
            Assert.That(decision, Is.Null);
            Assert.That(deadline, Is.EqualTo(22));
            Assert.That(selector.TryPrepare(3, Observed(guard: 0, focus: 0), out decision,
                out deadline), Is.False);
            Assert.That(deadline, Is.Null);
            Assert.That(selector.SelectionCount, Is.EqualTo(2));
        }

        [Test]
        public void EarliestOtherwiseEligibleCooldownWinsWithoutConsumingADraw()
        {
            var selector = new EnemyPatternSelector(DeckWithIntro("a",
                Pattern("a", cooldownUs: 10), Pattern("b", cooldownUs: 20)), 1);
            CommitNext(selector, 0);
            CommitNext(selector, 2);
            uint state = selector.RandomState;

            Assert.That(selector.TryPrepare(3, Observed(), out var decision, out var deadline), Is.False);
            Assert.That(decision, Is.Null);
            Assert.That(deadline, Is.EqualTo(10));
            Assert.That(selector.RandomState, Is.EqualTo(state));
            Assert.That(selector.History, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(Prepare(selector, 10).Pattern.Id, Is.EqualTo("a"));
        }

        [Test]
        public void ForeignNullStaleAndRepeatedDecisionsCannotMutateTheSelector()
        {
            var selector = new EnemyPatternSelector(WeightedDeck(), 1);
            var foreignSelector = new EnemyPatternSelector(selector.Deck, 1);
            var foreign = Prepare(foreignSelector, 0);
            var chosen = Prepare(selector, 0);
            var stale = Prepare(selector, 1);

            AssertRejectedWithoutMutation(selector, null, 0);
            AssertRejectedWithoutMutation(selector, foreign, 0);
            Assert.That(selector.TryCommit(chosen, 0), Is.True);
            AssertRejectedWithoutMutation(selector, chosen, 1);
            AssertRejectedWithoutMutation(selector, stale, 1);
        }

        [Test]
        public void AdmissionMustNotPrecedePreparationOrTheLastSuccessfulCommit()
        {
            var selector = new EnemyPatternSelector(WeightedDeck(), 1);
            var decision = Prepare(selector, 100);

            AssertRejectedWithoutMutation(selector, decision, -1);
            AssertRejectedWithoutMutation(selector, decision, 99);
            Assert.That(selector.TryCommit(decision, 200), Is.True);
            var following = Prepare(selector, 200);
            AssertRejectedWithoutMutation(selector, following, 199);
            Assert.That(selector.TryCommit(following, 200), Is.True);
        }

        [Test]
        public void CooldownOverflowRejectsBeforeAnySelectionStateChanges()
        {
            var selector = new EnemyPatternSelector(Deck(Pattern("a", cooldownUs: 1)), 1);
            var decision = Prepare(selector, 0);

            AssertRejectedWithoutMutation(selector, decision, long.MaxValue);
            Assert.That(selector.CanCommit(decision, long.MaxValue - 1), Is.True);
            Assert.That(selector.TryCommit(decision, long.MaxValue - 1), Is.True);
            Assert.That(selector.TryPrepare(long.MaxValue - 1, Observed(), out var early,
                out var deadline), Is.False);
            Assert.That(early, Is.Null);
            Assert.That(deadline, Is.EqualTo(long.MaxValue));
            AssertRejectedWithoutMutation(selector, Prepare(selector, long.MaxValue), long.MaxValue);
        }

        [Test]
        public void ZeroCooldownSupportsTheLargestCombatTimestamp()
        {
            var selector = new EnemyPatternSelector(Deck(Pattern("a")), 1);
            var decision = Prepare(selector, long.MaxValue);

            Assert.That(selector.CanCommit(decision, long.MaxValue), Is.True);
            Assert.That(selector.TryCommit(decision, long.MaxValue), Is.True);
            Assert.That(Prepare(selector, long.MaxValue).Pattern.Id, Is.EqualTo("a"));
        }

        [Test]
        public void NegativePreparationTimeRejectsWithoutMutation()
        {
            var selector = new EnemyPatternSelector(WeightedDeck(), 1);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                selector.TryPrepare(-1, Observed(), out _, out _));
            Assert.That(selector.RandomState, Is.EqualTo(1u));
            Assert.That(selector.SelectionCount, Is.Zero);
            Assert.That(selector.History, Is.Empty);
        }

        private static void AssertLiteralSequence(EnemyPatternSelector selector, string[] ids, uint[] states)
        {
            for (int index = 0; index < ids.Length; index++)
            {
                var decision = Prepare(selector, index);
                Assert.That(decision.Pattern.Id, Is.EqualTo(ids[index]), "selection " + index);
                Assert.That(decision.SelectionIndex, Is.EqualTo(index));
                Assert.That(decision.RandomStateBefore, Is.EqualTo(selector.RandomState));
                Assert.That(decision.RandomStateAfter, Is.EqualTo(states[index]));
                Assert.That(selector.CanCommit(decision, index), Is.True);
                Assert.That(selector.TryCommit(decision, index), Is.True);
                Assert.That(selector.SelectionCount, Is.EqualTo(index + 1));
                Assert.That(selector.RandomState, Is.EqualTo(states[index]));
            }
        }

        private static void AssertRejectedWithoutMutation(EnemyPatternSelector selector,
            EnemyPatternDecision decision, long timeUs)
        {
            uint randomState = selector.RandomState;
            int count = selector.SelectionCount;
            var history = selector.History;
            Assert.That(selector.CanCommit(decision, timeUs), Is.False);
            Assert.That(selector.TryCommit(decision, timeUs), Is.False);
            Assert.That(selector.RandomState, Is.EqualTo(randomState));
            Assert.That(selector.SelectionCount, Is.EqualTo(count));
            Assert.That(selector.History, Is.EqualTo(history));
        }

        private static EnemyPatternDecision CommitNext(EnemyPatternSelector selector, long timeUs)
        {
            var decision = Prepare(selector, timeUs);
            Assert.That(selector.TryCommit(decision, timeUs), Is.True);
            return decision;
        }

        private static EnemyPatternDecision Prepare(EnemyPatternSelector selector, long timeUs,
            EnemyObservation? observation = null)
        {
            Assert.That(selector.TryPrepare(timeUs, observation ?? Observed(), out var decision,
                out var deadline), Is.True);
            Assert.That(deadline, Is.Null);
            return decision;
        }

        private static EnemyObservation Observed(int guard = 100, int dodgeCharges = 2,
            int focus = 100, DefenseOutcome? outcome = null)
        {
            return new EnemyObservation(100, guard, dodgeCharges, focus, outcome);
        }

        private static EnemyPatternDeck WeightedDeck()
        {
            return Deck(Pattern("a", 100), Pattern("b", 200), Pattern("c", 300));
        }

        private static EnemyPatternDeck Deck(params EnemyPattern[] patterns)
        {
            return DeckWithIntro(null, patterns);
        }

        private static EnemyPatternDeck DeckWithIntro(string introId, params EnemyPattern[] patterns)
        {
            return new EnemyPatternDeck("test.deck", EnemyArchetype.Sword, "test lesson",
                "content.v1", "balance.v1", patterns, introId);
        }

        private static EnemyPattern Pattern(string id, int weight = 100, long cooldownUs = 0,
            int minimumGuard = 0, int minimumDodgeCharges = 0, int minimumFocus = 0,
            int minimumTier = 0, int maximumTier = 5, DefenseOutcome? preferredDefenseOutcome = null)
        {
            var step = new EnemyPatternStep(id + ".attack", DefenseMask.Guard | DefenseMask.Parry |
                DefenseMask.Dodge, DodgeSide.Left, SwipeDirection.Up, 25, 15);
            return new EnemyPattern(id, new[] { step }, weight, cooldownUs, minimumGuard,
                minimumDodgeCharges, minimumFocus, minimumTier, maximumTier, preferredDefenseOutcome);
        }
    }
}
