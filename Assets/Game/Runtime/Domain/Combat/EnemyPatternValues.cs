using System;
using System.Collections.Generic;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Domain.Combat
{
    public enum EnemyArchetype { Sword, Shield, Polearm, Hammer }

    /// <summary>Completed gameplay observations approved for authored enemy selection.</summary>
    public readonly struct EnemyObservation
    {
        public int PlayerHealth { get; }
        public int Guard { get; }
        public int DodgeCharges { get; }
        public int Focus { get; }
        public DefenseOutcome? LastDefenseOutcome { get; }
        public SwipeDirection? LastAttackDirection { get; }

        public EnemyObservation(int playerHealth, int guard, int dodgeCharges, int focus,
            DefenseOutcome? lastDefenseOutcome = null, SwipeDirection? lastAttackDirection = null)
        {
            if (playerHealth < 0) throw new ArgumentOutOfRangeException(nameof(playerHealth));
            if (guard < 0) throw new ArgumentOutOfRangeException(nameof(guard));
            if (dodgeCharges < 0) throw new ArgumentOutOfRangeException(nameof(dodgeCharges));
            if (focus < 0) throw new ArgumentOutOfRangeException(nameof(focus));
            if (lastDefenseOutcome.HasValue && !Enum.IsDefined(typeof(DefenseOutcome), lastDefenseOutcome.Value))
                throw new ArgumentOutOfRangeException(nameof(lastDefenseOutcome));
            if (lastAttackDirection.HasValue && !Enum.IsDefined(typeof(SwipeDirection), lastAttackDirection.Value))
                throw new ArgumentOutOfRangeException(nameof(lastAttackDirection));
            PlayerHealth = playerHealth;
            Guard = guard;
            DodgeCharges = dodgeCharges;
            Focus = focus;
            LastDefenseOutcome = lastDefenseOutcome;
            LastAttackDirection = lastAttackDirection;
        }
    }

    /// <summary>Immutable attack and timing authored before a sequence is selected.</summary>
    public sealed class EnemyPatternStep
    {
        public string AttackId { get; }
        public DefenseMask AllowedDefenses { get; }
        public DodgeSide SafeDodgeSides { get; }
        public SwipeDirection ParryDirection { get; }
        public int GuardCost { get; }
        public int Damage { get; }
        public long TelegraphDurationUs { get; }
        public long RecoveryDurationUs { get; }
        public long GapBeforeUs { get; }

        public EnemyPatternStep(string attackId, DefenseMask allowedDefenses, DodgeSide safeDodgeSides,
            SwipeDirection parryDirection, int guardCost, int damage, long telegraphDurationUs = 650000,
            long recoveryDurationUs = 500000, long gapBeforeUs = 0)
        {
            EnemyPatternValidation.StableId(attackId, nameof(attackId));
            // The strike constructor is the authoritative defense-definition validator.
            _ = new EnemyStrike(1, allowedDefenses, safeDodgeSides, parryDirection, guardCost, damage,
                telegraphDurationUs, recoveryDurationUs);
            if (gapBeforeUs < 0) throw new ArgumentOutOfRangeException(nameof(gapBeforeUs));
            _ = checked(gapBeforeUs + telegraphDurationUs + recoveryDurationUs);
            AttackId = attackId;
            AllowedDefenses = allowedDefenses;
            SafeDodgeSides = safeDodgeSides;
            ParryDirection = parryDirection;
            GuardCost = guardCost;
            Damage = damage;
            TelegraphDurationUs = telegraphDurationUs;
            RecoveryDurationUs = recoveryDurationUs;
            GapBeforeUs = gapBeforeUs;
        }

        public EnemyStrike CreateStrike(long strikeId, long phaseId)
        {
            if (phaseId <= 0) throw new ArgumentOutOfRangeException(nameof(phaseId));
            return new EnemyStrike(strikeId, AllowedDefenses, SafeDodgeSides, ParryDirection,
                GuardCost, Damage, TelegraphDurationUs, RecoveryDurationUs, interruptible: false);
        }
    }

    /// <summary>A small committed sequence with authored selection conditions.</summary>
    public sealed class EnemyPattern
    {
        public string Id { get; }
        public IReadOnlyList<EnemyPatternStep> Steps { get; }
        public int Weight { get; }
        public long CooldownUs { get; }
        public int MinimumGuard { get; }
        public int MinimumDodgeCharges { get; }
        public int MinimumFocus { get; }
        public int MinimumTier { get; }
        public int MaximumTier { get; }
        public DefenseOutcome? PreferredDefenseOutcome { get; }

        public EnemyPattern(string id, IReadOnlyList<EnemyPatternStep> steps, int weight = 100,
            long cooldownUs = 0, int minimumGuard = 0, int minimumDodgeCharges = 0,
            int minimumFocus = 0, int minimumTier = 0, int maximumTier = 5,
            DefenseOutcome? preferredDefenseOutcome = null)
        {
            EnemyPatternValidation.StableId(id, nameof(id));
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            if (steps.Count < 1 || steps.Count > 3)
                throw new ArgumentException("A regular pattern requires one to three steps.", nameof(steps));
            if (weight < 1 || weight > 1000000) throw new ArgumentOutOfRangeException(nameof(weight));
            if (cooldownUs < 0) throw new ArgumentOutOfRangeException(nameof(cooldownUs));
            if (minimumGuard < 0) throw new ArgumentOutOfRangeException(nameof(minimumGuard));
            if (minimumDodgeCharges < 0) throw new ArgumentOutOfRangeException(nameof(minimumDodgeCharges));
            if (minimumFocus < 0) throw new ArgumentOutOfRangeException(nameof(minimumFocus));
            if (minimumTier < 0 || minimumTier > 5) throw new ArgumentOutOfRangeException(nameof(minimumTier));
            if (maximumTier < minimumTier || maximumTier > 5)
                throw new ArgumentOutOfRangeException(nameof(maximumTier));
            if (preferredDefenseOutcome.HasValue &&
                !Enum.IsDefined(typeof(DefenseOutcome), preferredDefenseOutcome.Value))
                throw new ArgumentOutOfRangeException(nameof(preferredDefenseOutcome));
            var copy = CopySteps(steps);
            Id = id;
            Steps = Array.AsReadOnly(copy);
            Weight = weight;
            CooldownUs = cooldownUs;
            MinimumGuard = minimumGuard;
            MinimumDodgeCharges = minimumDodgeCharges;
            MinimumFocus = minimumFocus;
            MinimumTier = minimumTier;
            MaximumTier = maximumTier;
            PreferredDefenseOutcome = preferredDefenseOutcome;
        }

        public bool IsEligible(EnemyObservation observation, int tier)
        {
            if (tier < 0 || tier > 5) throw new ArgumentOutOfRangeException(nameof(tier));
            return tier >= MinimumTier && tier <= MaximumTier &&
                observation.Guard >= MinimumGuard && observation.DodgeCharges >= MinimumDodgeCharges &&
                observation.Focus >= MinimumFocus;
        }

        private static EnemyPatternStep[] CopySteps(IReadOnlyList<EnemyPatternStep> steps)
        {
            var copy = new EnemyPatternStep[steps.Count];
            long durationUs = 0;
            for (int i = 0; i < copy.Length; i++)
            {
                var step = steps[i];
                if (step == null) throw new ArgumentException("Pattern steps cannot be null.", nameof(steps));
                durationUs = checked(durationUs + step.GapBeforeUs + step.TelegraphDurationUs + step.RecoveryDurationUs);
                copy[i] = step;
            }
            return copy;
        }
    }

    /// <summary>Copied encounter content with stable identity and captured revisions.</summary>
    public sealed class EnemyPatternDeck
    {
        public string Id { get; }
        public EnemyArchetype Archetype { get; }
        public string Lesson { get; }
        public string ContentRevision { get; }
        public string BalanceRevision { get; }
        public IReadOnlyList<EnemyPattern> Patterns { get; }
        public string IntroPatternId { get; }

        public EnemyPatternDeck(string id, EnemyArchetype archetype, string lesson, string contentRevision,
            string balanceRevision, IReadOnlyList<EnemyPattern> patterns, string introPatternId = null)
        {
            EnemyPatternValidation.StableId(id, nameof(id));
            if (!Enum.IsDefined(typeof(EnemyArchetype), archetype))
                throw new ArgumentOutOfRangeException(nameof(archetype));
            EnemyPatternValidation.Text(lesson, nameof(lesson));
            EnemyPatternValidation.Text(contentRevision, nameof(contentRevision));
            EnemyPatternValidation.Text(balanceRevision, nameof(balanceRevision));
            if (patterns == null) throw new ArgumentNullException(nameof(patterns));
            if (patterns.Count < 1 || patterns.Count > 16)
                throw new ArgumentException("A regular deck requires one to sixteen patterns.", nameof(patterns));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var copy = CopyPatterns(patterns, ids);
            if (introPatternId != null)
            {
                EnemyPatternValidation.StableId(introPatternId, nameof(introPatternId));
                if (!ids.Contains(introPatternId))
                    throw new ArgumentException("The intro must identify a pattern in this deck.", nameof(introPatternId));
            }
            Id = id;
            Archetype = archetype;
            Lesson = lesson;
            ContentRevision = contentRevision;
            BalanceRevision = balanceRevision;
            Patterns = Array.AsReadOnly(copy);
            IntroPatternId = introPatternId;
        }

        private static EnemyPattern[] CopyPatterns(IReadOnlyList<EnemyPattern> patterns, HashSet<string> ids)
        {
            var copy = new EnemyPattern[patterns.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                var pattern = patterns[i];
                if (pattern == null) throw new ArgumentException("Deck patterns cannot be null.", nameof(patterns));
                if (!ids.Add(pattern.Id))
                    throw new ArgumentException("Deck pattern identities must be distinct.", nameof(patterns));
                copy[i] = pattern;
            }
            return copy;
        }
    }

    internal static class EnemyPatternValidation
    {
        internal static void StableId(string value, string parameter)
        {
            Text(value, parameter);
            foreach (char character in value)
                if (char.IsWhiteSpace(character))
                    throw new ArgumentException("Content identities cannot contain whitespace.", parameter);
        }

        internal static void Text(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Content metadata cannot be empty.", parameter);
        }
    }
}
