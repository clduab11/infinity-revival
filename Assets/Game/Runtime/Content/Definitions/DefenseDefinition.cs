using Praxen.Game.Domain.Combat;
using UnityEngine;

namespace Praxen.Game.Content.Definitions
{
    [CreateAssetMenu(menuName = "Praxen/Content/Defense")]
    public sealed class DefenseDefinition : ContentDefinition
    {
        public int MaximumHealth = 100, MaximumGuard = 100, MaximumDodgeCharges = 3;
        public long GuardStaggerUs = 600000, DodgeDurationUs = 360000;
        public long DodgeAvoidanceStartUs = 50000, DodgeAvoidanceEndUs = 230000;
        public long ParryWindowUs = 140000, ParryRecoveryUs = 250000, HitRecoveryUs = 200000;
        public long DodgeRechargeUs = 3000000;

        public CombatDefenseTuning ToRuntime()
        { Validate().ThrowIfInvalid(); return BuildRuntime(); }
        internal CombatDefenseTuning BuildRuntime() => new CombatDefenseTuning(MaximumHealth,
            MaximumGuard, MaximumDodgeCharges, GuardStaggerUs, DodgeDurationUs,
            DodgeAvoidanceStartUs, DodgeAvoidanceEndUs, ParryWindowUs, ParryRecoveryUs,
            HitRecoveryUs, DodgeRechargeUs);
        internal override void ValidateFields(ContentValidationContext context)
            => context.Attempt(this, "Defense", () => BuildRuntime());
    }
}
