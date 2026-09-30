using System;
using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Content.Combat;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class EnemyPatternDefinitionTests
    {
        [TestCase(-1, 0, 0, 0)]
        [TestCase(0, -1, 0, 0)]
        [TestCase(0, 0, -1, 0)]
        [TestCase(0, 0, 0, -1)]
        public void ObservationsRejectNegativeResources(int health, int guard, int dodges, int focus)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EnemyObservation(health, guard, dodges, focus));
        }

        [Test]
        public void ObservationsRejectUnknownCompletedOutcomesOrDirections()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EnemyObservation(100, 100, 3, 0, (DefenseOutcome)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EnemyObservation(100, 100, 3, 0, lastAttackDirection: (SwipeDirection)99));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("attack with spaces")]
        public void StepsRequireStableAttackIdentity(string id)
        {
            Assert.Throws<ArgumentException>(() => Step(id));
        }

        [TestCase(DefenseMask.None, DodgeSide.None, SwipeDirection.Up, 20, 25)]
        [TestCase((DefenseMask)8, DodgeSide.None, SwipeDirection.Up, 20, 25)]
        [TestCase(DefenseMask.Dodge, DodgeSide.None, SwipeDirection.Up, 0, 25)]
        [TestCase(DefenseMask.Dodge, (DodgeSide)4, SwipeDirection.Up, 0, 25)]
        [TestCase(DefenseMask.Parry, DodgeSide.Left, SwipeDirection.Up, 0, 25)]
        [TestCase(DefenseMask.Parry, DodgeSide.None, (SwipeDirection)99, 0, 25)]
        [TestCase(DefenseMask.Guard, DodgeSide.None, SwipeDirection.Up, 0, 25)]
        [TestCase(DefenseMask.Parry, DodgeSide.None, SwipeDirection.Up, -1, 25)]
        [TestCase(DefenseMask.Parry, DodgeSide.None, SwipeDirection.Up, 0, -1)]
        public void StepsRejectMalformedDefenseDefinitions(DefenseMask defenses, DodgeSide sides,
            SwipeDirection direction, int guardCost, int damage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EnemyPatternStep("attack", defenses, sides, direction, guardCost, damage));
        }

        [TestCase(0, 0, 0)]
        [TestCase(-1, 0, 0)]
        [TestCase(650000, -1, 0)]
        [TestCase(650000, 0, -1)]
        public void StepsRejectInvalidTiming(long telegraphUs, long recoveryUs, long gapUs)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Step(telegraphUs: telegraphUs,
                recoveryUs: recoveryUs, gapUs: gapUs));
        }

        [Test]
        public void StepTimingOverflowIsRejectedBeforeScheduling()
        {
            Assert.Throws<OverflowException>(() => Step(telegraphUs: long.MaxValue, recoveryUs: 1));
            Assert.Throws<OverflowException>(() => Step(telegraphUs: 1, recoveryUs: 0,
                gapUs: long.MaxValue));
        }

        [Test]
        public void AuthoredStepCreatesAnExactNoninterruptibleStrike()
        {
            var step = new EnemyPatternStep("high-cut", DefenseMask.Parry | DefenseMask.Dodge,
                DodgeSide.Right, SwipeDirection.Down, 0, 37, 900000, 700000, 120000);

            var strike = step.CreateStrike(42, 7);

            Assert.That(strike.Id, Is.EqualTo(42));
            Assert.That(strike.AllowedDefenses, Is.EqualTo(DefenseMask.Parry | DefenseMask.Dodge));
            Assert.That(strike.SafeDodgeSides, Is.EqualTo(DodgeSide.Right));
            Assert.That(strike.RequiredParryDirection, Is.EqualTo(SwipeDirection.Down));
            Assert.That(strike.GuardCost, Is.Zero);
            Assert.That(strike.HealthDamage, Is.EqualTo(37));
            Assert.That(strike.TelegraphDurationUs, Is.EqualTo(900000));
            Assert.That(strike.RecoveryDurationUs, Is.EqualTo(700000));
            Assert.That(strike.Interruptible, Is.False);
            Assert.That(step.GapBeforeUs, Is.EqualTo(120000));
        }

        [TestCase(0, 1)]
        [TestCase(-1, 1)]
        [TestCase(1, 0)]
        [TestCase(1, -1)]
        public void StrikeCreationRequiresPositiveStrikeAndPhaseIdentity(long strikeId, long phaseId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Step().CreateStrike(strikeId, phaseId));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("pattern with spaces")]
        public void PatternsRequireStableIdentity(string id)
        {
            Assert.Throws<ArgumentException>(() => Pattern(id));
        }

        [Test]
        public void PatternsRequireOneToThreeNonNullSteps()
        {
            Assert.Throws<ArgumentNullException>(() => new EnemyPattern("pattern", null));
            Assert.Throws<ArgumentException>(() => new EnemyPattern("pattern", Array.Empty<EnemyPatternStep>()));
            Assert.Throws<ArgumentException>(() => new EnemyPattern("pattern", new EnemyPatternStep[] { null }));
            Assert.Throws<ArgumentException>(() => new EnemyPattern("pattern",
                new[] { Step(), Step(), Step(), Step() }));
        }

        [TestCase(0, 0, 0, 0, 0, 0, 5)]
        [TestCase(-1, 0, 0, 0, 0, 0, 5)]
        [TestCase(1000001, 0, 0, 0, 0, 0, 5)]
        [TestCase(1, -1, 0, 0, 0, 0, 5)]
        [TestCase(1, 0, -1, 0, 0, 0, 5)]
        [TestCase(1, 0, 0, -1, 0, 0, 5)]
        [TestCase(1, 0, 0, 0, -1, 0, 5)]
        [TestCase(1, 0, 0, 0, 0, -1, 5)]
        [TestCase(1, 0, 0, 0, 0, 0, 6)]
        [TestCase(1, 0, 0, 0, 0, 3, 2)]
        public void PatternsRejectInvalidWeightCooldownResourcesOrTierRange(int weight, long cooldown,
            int guard, int dodges, int focus, int minimumTier, int maximumTier)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyPattern("pattern", new[] { Step() },
                weight, cooldown, guard, dodges, focus, minimumTier, maximumTier));
        }

        [Test]
        public void PatternsRejectUnknownPreferredCompletedOutcome()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyPattern("pattern", new[] { Step() },
                preferredDefenseOutcome: (DefenseOutcome)99));
        }

        [Test]
        public void PatternDurationOverflowIsRejectedAcrossOtherwiseValidSteps()
        {
            var first = Step(telegraphUs: long.MaxValue - 1, recoveryUs: 0);
            var second = Step(telegraphUs: 2, recoveryUs: 0);

            Assert.Throws<OverflowException>(() => new EnemyPattern("pattern", new[] { first, second }));
        }

        [Test]
        public void PatternCopiesItsStepsAndExposesNoMutableCollection()
        {
            var original = Step("original");
            var input = new List<EnemyPatternStep> { original };
            var pattern = new EnemyPattern("pattern", input);

            input[0] = Step("replacement");
            input.Add(Step("extra"));

            Assert.That(pattern.Steps.Count, Is.EqualTo(1));
            Assert.That(pattern.Steps[0].AttackId, Is.EqualTo("original"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<EnemyPatternStep>)pattern.Steps)[0] = Step("mutated"));
        }

        [TestCase(19, 2, 30, 2, false)]
        [TestCase(20, 1, 30, 2, false)]
        [TestCase(20, 2, 29, 2, false)]
        [TestCase(20, 2, 30, 1, false)]
        [TestCase(20, 2, 30, 5, false)]
        [TestCase(20, 2, 30, 2, true)]
        [TestCase(20, 2, 30, 4, true)]
        public void EligibilityUsesInclusiveApprovedResourceAndTierBounds(int guard, int dodges,
            int focus, int tier, bool eligible)
        {
            var pattern = new EnemyPattern("pattern", new[] { Step() }, minimumGuard: 20,
                minimumDodgeCharges: 2, minimumFocus: 30, minimumTier: 2, maximumTier: 4);

            Assert.That(pattern.IsEligible(new EnemyObservation(100, guard, dodges, focus), tier),
                Is.EqualTo(eligible));
        }

        [TestCase(-1)]
        [TestCase(6)]
        public void EligibilityRejectsDifficultyOutsideTheLaunchRange(int tier)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Pattern().IsEligible(default, tier));
        }

        [Test]
        public void PreferredDefenseOutcomeIsAWeightPreferenceAndNeverAnEligibilityGate()
        {
            var pattern = new EnemyPattern("pattern", new[] { Step() },
                preferredDefenseOutcome: DefenseOutcome.Parried);

            Assert.That(pattern.IsEligible(new EnemyObservation(100, 0, 0, 0), 0), Is.True);
            Assert.That(pattern.IsEligible(new EnemyObservation(100, 0, 0, 0, DefenseOutcome.Hit,
                SwipeDirection.Down), 5), Is.True);
        }

        [Test]
        public void DeckCopiesPatternsAndRetainsEncounterContentIdentity()
        {
            var input = new List<EnemyPattern> { Pattern("intro"), Pattern("follow-up") };
            var deck = new EnemyPatternDeck("sword-v1", EnemyArchetype.Sword, "Read direction",
                "content-v1", "balance-v2", input, "intro");

            input.Clear();

            Assert.That(deck.Patterns.Count, Is.EqualTo(2));
            Assert.That(deck.Patterns[0].Id, Is.EqualTo("intro"));
            Assert.That(deck.IntroPatternId, Is.EqualTo("intro"));
            Assert.That(deck.ContentRevision, Is.EqualTo("content-v1"));
            Assert.That(deck.BalanceRevision, Is.EqualTo("balance-v2"));
            Assert.Throws<NotSupportedException>(() => ((IList<EnemyPattern>)deck.Patterns).Clear());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("deck with spaces")]
        public void DecksRequireStableIdentity(string id)
        {
            Assert.Throws<ArgumentException>(() => Deck(id: id));
        }

        [Test]
        public void DecksRejectUnknownArchetypesMissingMetadataOrInvalidIntroIdentity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Deck(archetype: (EnemyArchetype)99));
            Assert.Throws<ArgumentException>(() => Deck(lesson: " "));
            Assert.Throws<ArgumentException>(() => Deck(contentRevision: " "));
            Assert.Throws<ArgumentException>(() => Deck(balanceRevision: ""));
            Assert.Throws<ArgumentException>(() => Deck(introId: "absent"));
            Assert.Throws<ArgumentException>(() => Deck(introId: " "));
        }

        [Test]
        public void DecksRequireOneToSixteenDistinctNonNullPatterns()
        {
            Assert.Throws<ArgumentNullException>(() => Deck(patterns: null));
            Assert.Throws<ArgumentException>(() => Deck(patterns: Array.Empty<EnemyPattern>()));
            Assert.Throws<ArgumentException>(() => Deck(patterns: new EnemyPattern[] { null }));
            Assert.Throws<ArgumentException>(() => Deck(patterns: new[] { Pattern(), Pattern() }));
            var oversized = new List<EnemyPattern>();
            for (int i = 0; i < 17; i++) oversized.Add(Pattern("pattern-" + i));
            Assert.Throws<ArgumentException>(() => Deck(patterns: oversized));
        }

        [Test]
        public void MaximumAuthoredWeightAndSixteenPatternsRemainRepresentable()
        {
            var patterns = new List<EnemyPattern>();
            for (int i = 0; i < 16; i++) patterns.Add(new EnemyPattern("pattern-" + i,
                new[] { Step() }, 1000000));

            Assert.That(Deck(patterns: patterns).Patterns.Count, Is.EqualTo(16));
        }

        [TestCase(EnemyArchetype.Sword, "Basic direction recognition")]
        [TestCase(EnemyArchetype.Shield, "Patient defense and recovery punishment")]
        [TestCase(EnemyArchetype.Polearm, "Safe dodge-side recognition")]
        [TestCase(EnemyArchetype.Hammer, "Guard conservation and heavy attacks")]
        public void EveryPrototypeHasThreePatternsItsLessonAndAnUnconditionalFallback(
            EnemyArchetype archetype, string lesson)
        {
            var deck = PrototypeEnemyDecks.Create(archetype);

            Assert.That(deck.Archetype, Is.EqualTo(archetype));
            Assert.That(deck.Lesson, Is.EqualTo(lesson));
            Assert.That(deck.Patterns.Count, Is.EqualTo(3));
            Assert.That(deck.ContentRevision, Is.Not.Null.And.Not.Empty);
            Assert.That(deck.BalanceRevision, Is.Not.Null.And.Not.Empty);
            Assert.That(deck.Patterns[0].Id, Is.EqualTo(deck.IntroPatternId));
            Assert.That(deck.Patterns[0].CooldownUs, Is.Zero);
            Assert.That(deck.Patterns[1].CooldownUs, Is.EqualTo(4000000));
            Assert.That(deck.Patterns[2].CooldownUs, Is.EqualTo(6000000));
            for (int tier = 0; tier <= 5; tier++)
                Assert.That(deck.Patterns[0].IsEligible(new EnemyObservation(100, 0, 0, 0), tier), Is.True);
            foreach (var pattern in deck.Patterns)
            {
                Assert.That(pattern.Steps.Count, Is.InRange(1, 3));
                foreach (var step in pattern.Steps)
                    Assert.That(step.TelegraphDurationUs, Is.GreaterThanOrEqualTo(650000));
            }
        }

        [TestCase(EnemyArchetype.Sword)]
        [TestCase(EnemyArchetype.Shield)]
        [TestCase(EnemyArchetype.Polearm)]
        [TestCase(EnemyArchetype.Hammer)]
        public void PrototypeIntroPreservesTask06FirstStrikeAndItsAbsoluteTimes(EnemyArchetype archetype)
        {
            var intro = PrototypeEnemyDecks.Create(archetype).Patterns[0];
            Assert.That(intro.Steps.Count, Is.EqualTo(1));
            var step = intro.Steps[0];
            var strike = step.CreateStrike(1, 1);

            Assert.That(step.GapBeforeUs, Is.Zero);
            Assert.That(strike.AllowedDefenses, Is.EqualTo(DefenseMask.Guard | DefenseMask.Parry | DefenseMask.Dodge));
            Assert.That(strike.SafeDodgeSides, Is.EqualTo(DodgeSide.Left));
            Assert.That(strike.RequiredParryDirection, Is.EqualTo(SwipeDirection.Up));
            Assert.That(strike.GuardCost, Is.EqualTo(20));
            Assert.That(strike.HealthDamage, Is.EqualTo(25));
            Assert.That(10000 + strike.TelegraphDurationUs, Is.EqualTo(660000));
            Assert.That(10000 + strike.TelegraphDurationUs + strike.RecoveryDurationUs, Is.EqualTo(1160000));
        }

        [Test]
        public void PrototypesHaveDistinctDeckIdentitiesAndCapturedSharedRevisions()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var first = PrototypeEnemyDecks.Create(EnemyArchetype.Sword);
            foreach (EnemyArchetype archetype in Enum.GetValues(typeof(EnemyArchetype)))
            {
                var deck = PrototypeEnemyDecks.Create(archetype);
                Assert.That(ids.Add(deck.Id), Is.True);
                Assert.That(deck.ContentRevision, Is.EqualTo(first.ContentRevision));
                Assert.That(deck.BalanceRevision, Is.EqualTo(first.BalanceRevision));
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => PrototypeEnemyDecks.Create((EnemyArchetype)99));
        }

        private static EnemyPatternStep Step(string id = "attack", long telegraphUs = 650000,
            long recoveryUs = 500000, long gapUs = 0) => new EnemyPatternStep(id,
                DefenseMask.Guard | DefenseMask.Parry | DefenseMask.Dodge, DodgeSide.Left,
                SwipeDirection.Up, 20, 25, telegraphUs, recoveryUs, gapUs);

        private static EnemyPattern Pattern(string id = "pattern") => new EnemyPattern(id, new[] { Step() });

        private static EnemyPatternDeck Deck(string id = "deck", EnemyArchetype archetype = EnemyArchetype.Sword,
            string lesson = "Read direction", string contentRevision = "content-v1",
            string balanceRevision = "balance-v1", string introId = null) => new EnemyPatternDeck(id,
                archetype, lesson, contentRevision, balanceRevision, new[] { Pattern() }, introId);

        private static EnemyPatternDeck Deck(IReadOnlyList<EnemyPattern> patterns) => new EnemyPatternDeck("deck",
            EnemyArchetype.Sword, "Read direction", "content-v1", "balance-v1", patterns);
    }
}
