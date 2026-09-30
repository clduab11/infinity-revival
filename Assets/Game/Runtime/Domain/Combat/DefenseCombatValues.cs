using System;

namespace Praxen.Game.Domain.Combat
{
    [Flags]
    public enum DefenseMask { None = 0, Guard = 1, Parry = 2, Dodge = 4 }

    [Flags]
    public enum DodgeSide { None = 0, Left = 1, Right = 2, Both = 3 }

    public enum PlayerCombatState { Ready, Guarding, Dodging, Parrying, Recovery, Staggered, Dead, Attacking }
    public enum DefenseOutcome { Hit, Blocked, Parried, Dodged, GuardBroken, IgnoredAfterDeath }

    public readonly struct DefenseResolution
    {
        public long StrikeId { get; }
        public long TimeUs { get; }
        public DefenseOutcome Outcome { get; }
        public int HealthDamage { get; }
        public int GuardSpent { get; }
        public int HealthAfter { get; }
        public int GuardAfter { get; }
        public bool IsDead { get; }

        internal DefenseResolution(long strikeId, long timeUs, DefenseOutcome outcome,
            int healthDamage, int guardSpent, int healthAfter, int guardAfter)
        {
            StrikeId = strikeId;
            TimeUs = timeUs;
            Outcome = outcome;
            HealthDamage = healthDamage;
            GuardSpent = guardSpent;
            HealthAfter = healthAfter;
            GuardAfter = guardAfter;
            IsDead = healthAfter == 0;
        }
    }
}
