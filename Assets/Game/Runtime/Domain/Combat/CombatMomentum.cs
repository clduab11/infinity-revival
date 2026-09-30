using System;

namespace Praxen.Game.Domain.Combat
{
    /// <summary>Encounter pressure and Focus driven exclusively by monotonic combat microseconds.</summary>
    public sealed class CombatMomentum
    {
        private readonly CombatOffenseTuning _tuning;
        private long _lastSuccessfulDefenseUs;
        private long _appliedDecaySteps;
        private bool _hasSuccessfulDefense;
        private bool _stopped;

        public long TimeUs { get; private set; }
        public int Balance { get; private set; }
        public int Focus { get; private set; }
        public bool PendingBalanceBreak { get; private set; }

        public CombatMomentum(CombatOffenseTuning tuning = null)
        {
            _tuning = tuning ?? new CombatOffenseTuning();
        }

        public void RecordDefense(DefenseResolution resolution)
        {
            AdvanceTo(resolution.TimeUs);
            if (_stopped) return;
            if (resolution.IsDead || resolution.Outcome == DefenseOutcome.IgnoredAfterDeath)
            {
                Stop();
                return;
            }
            int balanceGrant;
            int focusGrant;
            switch (resolution.Outcome)
            {
                case DefenseOutcome.Parried:
                    balanceGrant = _tuning.ParryBalance;
                    focusGrant = _tuning.ParryFocus;
                    break;
                case DefenseOutcome.Dodged:
                    balanceGrant = _tuning.DodgeBalance;
                    focusGrant = _tuning.DodgeFocus;
                    break;
                case DefenseOutcome.Blocked:
                    balanceGrant = _tuning.BlockBalance;
                    focusGrant = _tuning.BlockFocus;
                    break;
                default:
                    return;
            }
            _hasSuccessfulDefense = true;
            _lastSuccessfulDefenseUs = TimeUs;
            _appliedDecaySteps = 0;
            GrantBalance(balanceGrant);
            Focus = focusGrant >= _tuning.FocusMaximum - Focus
                ? _tuning.FocusMaximum : Focus + focusGrant;
        }

        public void AdvanceTo(long timeUs)
        {
            if (timeUs < 0 || timeUs < TimeUs)
                throw new ArgumentOutOfRangeException(nameof(timeUs));
            if (!_stopped && _hasSuccessfulDefense)
            {
                long elapsedUs = timeUs - _lastSuccessfulDefenseUs;
                long steps = elapsedUs <= _tuning.BalanceDecayDelayUs ? 0 :
                    (elapsedUs - _tuning.BalanceDecayDelayUs) / _tuning.BalanceDecayStepUs;
                long additionalSteps = steps - _appliedDecaySteps;
                Balance = additionalSteps >= Balance ? 0 : Balance - (int)additionalSteps;
                _appliedDecaySteps = steps;
            }
            TimeUs = timeUs;
        }

        public bool ConsumeBalanceBreak()
        {
            bool pending = PendingBalanceBreak;
            PendingBalanceBreak = false;
            return pending;
        }

        /// <summary>Terminal encounter stop retains resources and prevents future gain or decay.</summary>
        public void Stop()
        {
            _stopped = true;
        }

        private void GrantBalance(int gain)
        {
            if (gain >= _tuning.BalanceMaximum - Balance)
            {
                Balance = 0;
                PendingBalanceBreak = true;
            }
            else
            {
                Balance += gain;
            }
        }
    }
}
