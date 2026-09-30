using System;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Domain.Combat
{
    public sealed class EnemyStrike
    {
        public long Id { get; }
        public DefenseMask AllowedDefenses { get; }
        public DodgeSide SafeDodgeSides { get; }
        public SwipeDirection RequiredParryDirection { get; }
        public int GuardCost { get; }
        public int HealthDamage { get; }
        public long TelegraphDurationUs { get; }
        public long RecoveryDurationUs { get; }
        public bool Interruptible { get; }

        public EnemyStrike(long id, DefenseMask allowedDefenses, DodgeSide safeDodgeSides,
            SwipeDirection requiredParryDirection, int guardCost, int healthDamage,
            long telegraphDurationUs = 650000, long recoveryDurationUs = 500000,
            bool interruptible = false)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            int mask = (int)allowedDefenses;
            if (mask <= 0 || (mask & ~7) != 0)
                throw new ArgumentOutOfRangeException(nameof(allowedDefenses));
            bool allowsDodge = (allowedDefenses & DefenseMask.Dodge) != 0;
            int sides = (int)safeDodgeSides;
            if (allowsDodge ? sides < 1 || sides > 3 : sides != 0)
                throw new ArgumentOutOfRangeException(nameof(safeDodgeSides));
            if (!Enum.IsDefined(typeof(SwipeDirection), requiredParryDirection))
                throw new ArgumentOutOfRangeException(nameof(requiredParryDirection));
            if (guardCost < 0 || ((allowedDefenses & DefenseMask.Guard) != 0 && guardCost == 0))
                throw new ArgumentOutOfRangeException(nameof(guardCost));
            if (healthDamage < 0) throw new ArgumentOutOfRangeException(nameof(healthDamage));
            if (telegraphDurationUs <= 0) throw new ArgumentOutOfRangeException(nameof(telegraphDurationUs));
            if (recoveryDurationUs < 0) throw new ArgumentOutOfRangeException(nameof(recoveryDurationUs));
            Id = id;
            AllowedDefenses = allowedDefenses;
            SafeDodgeSides = safeDodgeSides;
            RequiredParryDirection = requiredParryDirection;
            GuardCost = guardCost;
            HealthDamage = healthDamage;
            TelegraphDurationUs = telegraphDurationUs;
            RecoveryDurationUs = recoveryDurationUs;
            Interruptible = interruptible;
        }
    }
}
