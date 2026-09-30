using System;

namespace Praxen.Game.Domain.Combat
{
    /// <summary>Editable prototype offense and momentum values, in integer combat microseconds.</summary>
    public sealed class CombatOffenseTuning
    {
        public long NormalOpeningUs { get; }
        public long BreakOpeningUs { get; }
        public long BufferUs { get; }
        public int EnemyHealth { get; }
        public int AttackDamage { get; }
        public long WindupUs { get; }
        public long RecoveryUs { get; }
        public int ComboBonusPercent { get; }
        public int BalanceMaximum { get; }
        public long BalanceDecayDelayUs { get; }
        public long BalanceDecayStepUs { get; }
        public int FocusMaximum { get; }
        public int ParryBalance { get; }
        public int DodgeBalance { get; }
        public int BlockBalance { get; }
        public int ParryFocus { get; }
        public int DodgeFocus { get; }
        public int BlockFocus { get; }

        public CombatOffenseTuning(long normalOpeningUs = 2000000, long breakOpeningUs = 3000000,
            long bufferUs = 120000, int enemyHealth = 100, int attackDamage = 20,
            long windupUs = 100000, long recoveryUs = 400000, int comboBonusPercent = 10,
            int balanceMaximum = 100, long balanceDecayDelayUs = 3000000,
            long balanceDecayStepUs = 100000, int focusMaximum = 100,
            int parryBalance = 25, int dodgeBalance = 10, int blockBalance = 5,
            int parryFocus = 25, int dodgeFocus = 10, int blockFocus = 5)
        {
            Positive(normalOpeningUs, nameof(normalOpeningUs));
            Positive(breakOpeningUs, nameof(breakOpeningUs));
            Positive(bufferUs, nameof(bufferUs));
            Positive(enemyHealth, nameof(enemyHealth));
            Nonnegative(attackDamage, nameof(attackDamage));
            Positive(windupUs, nameof(windupUs));
            Positive(recoveryUs, nameof(recoveryUs));
            Nonnegative(comboBonusPercent, nameof(comboBonusPercent));
            Positive(balanceMaximum, nameof(balanceMaximum));
            Nonnegative(balanceDecayDelayUs, nameof(balanceDecayDelayUs));
            Positive(balanceDecayStepUs, nameof(balanceDecayStepUs));
            Positive(focusMaximum, nameof(focusMaximum));
            Nonnegative(parryBalance, nameof(parryBalance));
            Nonnegative(dodgeBalance, nameof(dodgeBalance));
            Nonnegative(blockBalance, nameof(blockBalance));
            Nonnegative(parryFocus, nameof(parryFocus));
            Nonnegative(dodgeFocus, nameof(dodgeFocus));
            Nonnegative(blockFocus, nameof(blockFocus));
            checked
            {
                _ = windupUs + recoveryUs;
                _ = (int)(attackDamage + (long)attackDamage * comboBonusPercent / 100);
            }
            NormalOpeningUs = normalOpeningUs;
            BreakOpeningUs = breakOpeningUs;
            BufferUs = bufferUs;
            EnemyHealth = enemyHealth;
            AttackDamage = attackDamage;
            WindupUs = windupUs;
            RecoveryUs = recoveryUs;
            ComboBonusPercent = comboBonusPercent;
            BalanceMaximum = balanceMaximum;
            BalanceDecayDelayUs = balanceDecayDelayUs;
            BalanceDecayStepUs = balanceDecayStepUs;
            FocusMaximum = focusMaximum;
            ParryBalance = parryBalance;
            DodgeBalance = dodgeBalance;
            BlockBalance = blockBalance;
            ParryFocus = parryFocus;
            DodgeFocus = dodgeFocus;
            BlockFocus = blockFocus;
        }

        private static void Positive(long value, string name)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(name);
        }

        private static void Nonnegative(long value, string name)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(name);
        }
    }
}
