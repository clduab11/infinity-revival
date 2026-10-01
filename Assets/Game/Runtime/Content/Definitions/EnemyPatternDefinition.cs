using System;
using Praxen.Game.Domain.Combat;
using UnityEngine;

namespace Praxen.Game.Content.Definitions
{
    [Serializable]
    public struct PatternStepBinding
    {
        public EnemyAttackDefinition Attack;
        public long GapBeforeUs;
    }

    [CreateAssetMenu(menuName = "Praxen/Content/Enemy Pattern")]
    public sealed class EnemyPatternDefinition : ContentDefinition
    {
        public PatternStepBinding[] Steps = Array.Empty<PatternStepBinding>();
        public int Weight = 100;
        public long CooldownUs;
        public int MinimumGuard, MinimumDodgeCharges, MinimumFocus, MinimumTier;
        public int MaximumTier = 5;
        public bool HasPreferredDefenseOutcome;
        public DefenseOutcome PreferredDefenseOutcome;

        public EnemyPattern ToRuntime()
        { Validate().ThrowIfInvalid(); return BuildRuntime(); }
        internal EnemyPattern BuildRuntime()
        {
            var steps = new EnemyPatternStep[Steps.Length];
            for (int i = 0; i < steps.Length; i++) steps[i] = Steps[i].Attack.BuildRuntime(Steps[i].GapBeforeUs);
            return new EnemyPattern(Id, steps, Weight, CooldownUs, MinimumGuard,
                MinimumDodgeCharges, MinimumFocus, MinimumTier, MaximumTier,
                HasPreferredDefenseOutcome ? PreferredDefenseOutcome : (DefenseOutcome?)null);
        }

        internal override void ValidateFields(ContentValidationContext context)
        {
            context.Require(Steps != null && Steps.Length >= 1 && Steps.Length <= 3, this, "Steps",
                "A regular pattern requires one to three steps.");
            if (Steps == null) return;
            bool complete = true;
            for (int i = 0; i < Steps.Length; i++)
            {
                context.Child(this, "Steps[" + i + "].Attack", Steps[i].Attack);
                context.Require(Steps[i].GapBeforeUs >= 0, this, "Steps[" + i + "].GapBeforeUs", "Gap cannot be negative.");
                complete &= Steps[i].Attack != null;
            }
            if (complete) context.Attempt(this, "Steps", () => BuildRuntime());
        }
    }
}
