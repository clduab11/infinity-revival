using System;

namespace Praxen.Game.Domain.Combat
{
    /// <summary>Prototype resources and microsecond timing. Avoidance endpoints are inclusive.</summary>
    public sealed class CombatDefenseTuning
    {
        public int MaximumHealth { get; }
        public int MaximumGuard { get; }
        public int MaximumDodgeCharges { get; }
        public long GuardStaggerUs { get; }
        public long DodgeDurationUs { get; }
        public long DodgeAvoidanceStartUs { get; }
        public long DodgeAvoidanceEndUs { get; }
        public long ParryWindowUs { get; }
        public long ParryRecoveryUs { get; }
        public long HitRecoveryUs { get; }
        public long DodgeRechargeUs { get; }

        public CombatDefenseTuning(int maximumHealth = 100, int maximumGuard = 100,
            int maximumDodgeCharges = 3, long guardStaggerUs = 600000,
            long dodgeDurationUs = 360000, long dodgeAvoidanceStartUs = 50000,
            long dodgeAvoidanceEndUs = 230000, long parryWindowUs = 140000,
            long parryRecoveryUs = 250000, long hitRecoveryUs = 200000,
            long dodgeRechargeUs = 3000000)
        {
            Positive(maximumHealth, nameof(maximumHealth));
            Positive(maximumGuard, nameof(maximumGuard));
            Positive(maximumDodgeCharges, nameof(maximumDodgeCharges));
            Positive(guardStaggerUs, nameof(guardStaggerUs));
            Positive(dodgeDurationUs, nameof(dodgeDurationUs));
            if (dodgeAvoidanceStartUs < 0 || dodgeAvoidanceStartUs > dodgeAvoidanceEndUs)
                throw new ArgumentOutOfRangeException(nameof(dodgeAvoidanceStartUs));
            if (dodgeAvoidanceEndUs >= dodgeDurationUs)
                throw new ArgumentOutOfRangeException(nameof(dodgeAvoidanceEndUs));
            Positive(parryWindowUs, nameof(parryWindowUs));
            Positive(parryRecoveryUs, nameof(parryRecoveryUs));
            Positive(hitRecoveryUs, nameof(hitRecoveryUs));
            Positive(dodgeRechargeUs, nameof(dodgeRechargeUs));
            MaximumHealth = maximumHealth;
            MaximumGuard = maximumGuard;
            MaximumDodgeCharges = maximumDodgeCharges;
            GuardStaggerUs = guardStaggerUs;
            DodgeDurationUs = dodgeDurationUs;
            DodgeAvoidanceStartUs = dodgeAvoidanceStartUs;
            DodgeAvoidanceEndUs = dodgeAvoidanceEndUs;
            ParryWindowUs = parryWindowUs;
            ParryRecoveryUs = parryRecoveryUs;
            HitRecoveryUs = hitRecoveryUs;
            DodgeRechargeUs = dodgeRechargeUs;
        }

        private static void Positive(long value, string name)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(name);
        }
    }
}
