using System;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Content.Combat
{
    /// <summary>Code-authored teaching fixtures until Task 13 introduces asset authoring.</summary>
    public static class PrototypeEnemyDecks
    {
        private const string ContentRevision = "prototype-enemy-decks-v1";
        private const string BalanceRevision = "task08-balance-2026-09-29-v1";

        public static EnemyPatternDeck Create(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Sword: return Sword();
                case EnemyArchetype.Shield: return Shield();
                case EnemyArchetype.Polearm: return Polearm();
                case EnemyArchetype.Hammer: return Hammer();
                default: throw new ArgumentOutOfRangeException(nameof(archetype));
            }
        }

        private static EnemyPatternDeck Sword() => Deck("sword", EnemyArchetype.Sword,
            "Basic direction recognition", new[]
            {
                Intro("sword"),
                new EnemyPattern("sword.side-pair", new[]
                {
                    Step("sword.left-cut", SwipeDirection.Left, DodgeSide.Right, 20, 25),
                    Step("sword.right-cut", SwipeDirection.Right, DodgeSide.Left, 20, 25, gapUs: 100000)
                }, weight: 80, cooldownUs: 4000000),
                new EnemyPattern("sword.cardinal-chain", new[]
                {
                    Step("sword.high-cut", SwipeDirection.Up, DodgeSide.Left, 25, 25),
                    Step("sword.low-cut", SwipeDirection.Down, DodgeSide.Right, 25, 25, gapUs: 100000),
                    Step("sword.left-finish", SwipeDirection.Left, DodgeSide.Right, 25, 30, recoveryUs: 650000)
                }, weight: 50, cooldownUs: 6000000, minimumTier: 1,
                    preferredDefenseOutcome: DefenseOutcome.Parried)
            });

        private static EnemyPatternDeck Shield() => Deck("shield", EnemyArchetype.Shield,
            "Patient defense and recovery punishment", new[]
            {
                Intro("shield"),
                new EnemyPattern("shield.patient-tell", new[]
                {
                    Step("shield.delayed-cut", SwipeDirection.Down, DodgeSide.Right, 30, 30,
                        telegraphUs: 1000000, recoveryUs: 950000)
                }, weight: 80, cooldownUs: 4000000),
                new EnemyPattern("shield.recovery-pair", new[]
                {
                    Step("shield.measured-cut", SwipeDirection.Left, DodgeSide.Right, 30, 30,
                        telegraphUs: 850000, recoveryUs: 600000),
                    Step("shield.exposed-finish", SwipeDirection.Up, DodgeSide.Left, 35, 35,
                        telegraphUs: 900000, recoveryUs: 1200000, gapUs: 150000)
                }, weight: 50, cooldownUs: 6000000, minimumTier: 1,
                    preferredDefenseOutcome: DefenseOutcome.Blocked)
            });

        private static EnemyPatternDeck Polearm() => Deck("polearm", EnemyArchetype.Polearm,
            "Safe dodge-side recognition", new[]
            {
                Intro("polearm"),
                new EnemyPattern("polearm.side-thrust", new[]
                {
                    Step("polearm.right-safe-thrust", SwipeDirection.Left, DodgeSide.Right, 0, 30,
                        defenses: DefenseMask.Parry | DefenseMask.Dodge, telegraphUs: 750000)
                }, weight: 80, cooldownUs: 4000000),
                new EnemyPattern("polearm.alternating-thrusts", new[]
                {
                    Step("polearm.left-safe-thrust", SwipeDirection.Up, DodgeSide.Left, 0, 30,
                        defenses: DefenseMask.Parry | DefenseMask.Dodge, telegraphUs: 750000),
                    Step("polearm.right-safe-return", SwipeDirection.Down, DodgeSide.Right, 0, 35,
                        defenses: DefenseMask.Parry | DefenseMask.Dodge, telegraphUs: 800000,
                        recoveryUs: 750000, gapUs: 150000)
                }, weight: 50, cooldownUs: 6000000, minimumDodgeCharges: 1,
                    preferredDefenseOutcome: DefenseOutcome.Dodged)
            });

        private static EnemyPatternDeck Hammer() => Deck("hammer", EnemyArchetype.Hammer,
            "Guard conservation and heavy attacks", new[]
            {
                Intro("hammer"),
                new EnemyPattern("hammer.heavy-overhead", new[]
                {
                    Step("hammer.heavy-downstroke", SwipeDirection.Up, DodgeSide.Left, 60, 40,
                        telegraphUs: 1000000, recoveryUs: 850000)
                }, weight: 80, cooldownUs: 4000000, minimumGuard: 30),
                new EnemyPattern("hammer.pressure-pair", new[]
                {
                    Step("hammer.weighted-sweep", SwipeDirection.Right, DodgeSide.Left, 45, 35,
                        telegraphUs: 850000, recoveryUs: 550000),
                    Step("hammer.crushing-finish", SwipeDirection.Down, DodgeSide.Right, 65, 45,
                        telegraphUs: 1100000, recoveryUs: 1000000, gapUs: 200000)
                }, weight: 50, cooldownUs: 6000000, minimumTier: 1,
                    preferredDefenseOutcome: DefenseOutcome.GuardBroken)
            });

        private static EnemyPattern Intro(string archetype) => new EnemyPattern(archetype + ".intro",
            new[] { Step("teaching.overhead", SwipeDirection.Up, DodgeSide.Left, 20, 25) }, weight: 120);

        private static EnemyPatternDeck Deck(string id, EnemyArchetype archetype, string lesson,
            EnemyPattern[] patterns) => new EnemyPatternDeck("prototype." + id, archetype, lesson,
                ContentRevision, BalanceRevision, patterns, id + ".intro");

        private static EnemyPatternStep Step(string id, SwipeDirection parryDirection, DodgeSide sides,
            int guardCost, int damage, DefenseMask defenses = DefenseMask.Guard | DefenseMask.Parry | DefenseMask.Dodge,
            long telegraphUs = 650000, long recoveryUs = 500000, long gapUs = 0) => new EnemyPatternStep(id,
                defenses, sides, parryDirection, guardCost, damage, telegraphUs, recoveryUs, gapUs);
    }
}
