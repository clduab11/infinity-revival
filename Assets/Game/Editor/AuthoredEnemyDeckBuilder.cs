using System;
using System.Collections.Generic;
using System.Linq;
using Praxen.Game.Content.Combat;
using Praxen.Game.Content.Definitions;
using Praxen.Game.Domain.Combat;

namespace Praxen.Game.Editor
{
    /// <summary>Copies the teaching fixture into explicit assets without changing its balance or identities.</summary>
    internal static class AuthoredEnemyDeckBuilder
    {
        internal static EnemyDeckDefinition[] Build()
        {
            var attacks = new Dictionary<string, EnemyAttackDefinition>(StringComparer.Ordinal);
            return Enum.GetValues(typeof(EnemyArchetype)).Cast<EnemyArchetype>()
                .Select(archetype => Deck(PrototypeEnemyDecks.Create(archetype), attacks)).ToArray();
        }

        private static EnemyDeckDefinition Deck(EnemyPatternDeck source,
            IDictionary<string, EnemyAttackDefinition> attacks)
        {
            var deck = Task13ContentAuthoring.Asset<EnemyDeckDefinition>(source.Id);
            deck.Archetype = source.Archetype;
            deck.Lesson = source.Lesson;
            deck.ContentRevision = source.ContentRevision;
            deck.BalanceRevision = source.BalanceRevision;
            deck.IntroPatternId = source.IntroPatternId;
            deck.Patterns = source.Patterns.Select(pattern => Pattern(pattern, attacks)).ToArray();
            Task13ContentAuthoring.Save(deck);
            return deck;
        }

        private static EnemyPatternDefinition Pattern(EnemyPattern source,
            IDictionary<string, EnemyAttackDefinition> attacks)
        {
            var pattern = Task13ContentAuthoring.Asset<EnemyPatternDefinition>(source.Id);
            pattern.Weight = source.Weight;
            pattern.CooldownUs = source.CooldownUs;
            pattern.MinimumGuard = source.MinimumGuard;
            pattern.MinimumDodgeCharges = source.MinimumDodgeCharges;
            pattern.MinimumFocus = source.MinimumFocus;
            pattern.MinimumTier = source.MinimumTier;
            pattern.MaximumTier = source.MaximumTier;
            pattern.HasPreferredDefenseOutcome = source.PreferredDefenseOutcome.HasValue;
            pattern.PreferredDefenseOutcome = source.PreferredDefenseOutcome ?? default;
            pattern.Steps = source.Steps.Select(step => new PatternStepBinding
                { Attack = Attack(step, attacks), GapBeforeUs = step.GapBeforeUs }).ToArray();
            Task13ContentAuthoring.Save(pattern);
            return pattern;
        }

        private static EnemyAttackDefinition Attack(EnemyPatternStep source,
            IDictionary<string, EnemyAttackDefinition> attacks)
        {
            if (attacks.TryGetValue(source.AttackId, out var existing)) return existing;
            var attack = Task13ContentAuthoring.Asset<EnemyAttackDefinition>(source.AttackId);
            attack.AllowedDefenses = source.AllowedDefenses;
            attack.SafeDodgeSides = source.SafeDodgeSides;
            attack.ParryDirection = source.ParryDirection;
            attack.GuardCost = source.GuardCost;
            attack.Damage = source.Damage;
            attack.TelegraphDurationUs = source.TelegraphDurationUs;
            attack.RecoveryDurationUs = source.RecoveryDurationUs;
            attacks.Add(source.AttackId, attack);
            Task13ContentAuthoring.Save(attack);
            return attack;
        }
    }
}
