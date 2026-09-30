using System;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Domain.Combat
{
    public sealed class DefenseCombatant
    {
        private long _armedStrikeId;
        private long _armedImpactUs;
        private long _parryRecoveryEndsUs;
        private long _hitRecoveryEndsUs;
        private long _offenseImpactUs;
        private long _guardActionId, _guardSourceUs;
        private SwipeDirection _armedDirection;
        private bool _dodgeAction;
        private bool _hasRechargeOrigin;
        private long _rechargeOriginUs;
        private int _chargesAfterDodge;

        public CombatDefenseTuning Tuning { get; }
        public long TimeUs { get; private set; }
        public int Health { get; private set; }
        public int Guard { get; private set; }
        public int DodgeCharges { get; private set; }
        public PlayerCombatState State { get; private set; }
        public bool GuardHeld { get; private set; }
        public DodgeSide DodgeSide { get; private set; }
        public long DodgeStartedUs { get; private set; }
        public long ActionEndsUs { get; private set; }

        public DefenseCombatant(CombatDefenseTuning tuning = null)
        {
            Tuning = tuning ?? new CombatDefenseTuning();
            Health = Tuning.MaximumHealth;
            Guard = Tuning.MaximumGuard;
            DodgeCharges = Tuning.MaximumDodgeCharges;
            State = PlayerCombatState.Ready;
        }

        public bool ApplyControl(DefenseCommand command, long combatTimeUs)
        {
            ValidateTime(combatTimeUs);
            ValidateCommand(command);
            if (command.Kind == DefenseCommandKind.GuardRelease)
            {
                AdvanceTo(combatTimeUs);
                bool ownsGuard = command.GuardActionId != 0 ? command.GuardActionId == _guardActionId :
                    command.InputTimestampUs >= _guardSourceUs;
                if (ownsGuard)
                {
                    GuardHeld = false;
                    if (State == PlayerCombatState.Guarding) State = PlayerCombatState.Ready;
                }
                return true;
            }
            var state = StateAt(combatTimeUs);
            bool canAct = state == PlayerCombatState.Ready || state == PlayerCombatState.Guarding;
            bool dodge = command.Kind == DefenseCommandKind.DodgeLeft ||
                command.Kind == DefenseCommandKind.DodgeRight;
            bool accepted = canAct && (dodge ? ChargesAt(combatTimeUs) > 0 :
                command.Kind != DefenseCommandKind.GuardPress || Guard > 0);
            long dodgeEndsUs = 0;
            if (accepted && dodge)
            {
                dodgeEndsUs = checked(combatTimeUs + Tuning.DodgeDurationUs);
                checked { _ = combatTimeUs + Tuning.DodgeRechargeUs; }
            }
            AdvanceTo(combatTimeUs);
            if (!accepted) return false;
            if (dodge)
                BeginDodge(command.Kind, combatTimeUs, dodgeEndsUs);
            else
            {
                GuardHeld = command.Kind == DefenseCommandKind.GuardPress;
                _guardActionId = command.GuardActionId;
                _guardSourceUs = command.InputTimestampUs;
                State = GuardHeld ? PlayerCombatState.Guarding : PlayerCombatState.Ready;
            }
            return true;
        }

        public bool TryParry(EnemyStrike strike, long scheduledImpactTimeUs,
            SwipeDirection direction, long combatTimeUs)
        {
            ValidateTime(combatTimeUs);
            if (strike == null) throw new ArgumentNullException(nameof(strike));
            if (scheduledImpactTimeUs < 0)
                throw new ArgumentOutOfRangeException(nameof(scheduledImpactTimeUs));
            if (!Enum.IsDefined(typeof(SwipeDirection), direction))
                throw new ArgumentOutOfRangeException(nameof(direction));
            var state = StateAt(combatTimeUs);
            bool accepted = (state == PlayerCombatState.Ready || state == PlayerCombatState.Guarding) &&
                (strike.AllowedDefenses & DefenseMask.Parry) != 0 &&
                direction == strike.RequiredParryDirection && combatTimeUs <= scheduledImpactTimeUs &&
                scheduledImpactTimeUs - combatTimeUs <= Tuning.ParryWindowUs;
            long recoveryEndsUs = accepted ? checked(scheduledImpactTimeUs + Tuning.ParryRecoveryUs) : 0;
            AdvanceTo(combatTimeUs);
            if (!accepted) return false;
            GuardHeld = false;
            _armedStrikeId = strike.Id;
            _armedImpactUs = scheduledImpactTimeUs;
            _armedDirection = direction;
            _parryRecoveryEndsUs = recoveryEndsUs;
            State = PlayerCombatState.Parrying;
            ActionEndsUs = scheduledImpactTimeUs;
            return true;
        }

        public bool TryBeginOffense(long combatTimeUs, long impactTimeUs, long recoveryEndUs)
        {
            ValidateTime(combatTimeUs);
            if (impactTimeUs <= combatTimeUs) throw new ArgumentOutOfRangeException(nameof(impactTimeUs));
            if (recoveryEndUs <= impactTimeUs) throw new ArgumentOutOfRangeException(nameof(recoveryEndUs));
            var state = StateAt(combatTimeUs);
            bool accepted = state == PlayerCombatState.Ready || state == PlayerCombatState.Guarding;
            AdvanceTo(combatTimeUs);
            if (!accepted) return false;
            ReleaseHeldDefense();
            _dodgeAction = false;
            _offenseImpactUs = impactTimeUs;
            ActionEndsUs = recoveryEndUs;
            State = PlayerCombatState.Attacking;
            return true;
        }

        public DefenseResolution ResolveImpact(EnemyStrike strike, long combatTimeUs)
        {
            ValidateTime(combatTimeUs);
            if (strike == null) throw new ArgumentNullException(nameof(strike));
            var state = StateAt(combatTimeUs);
            var outcome = OutcomeAt(strike, combatTimeUs, state);
            long recoveryEndsUs = ImpactRecoveryEndsAt(strike, combatTimeUs, outcome);
            AdvanceTo(combatTimeUs);
            int damage = outcome == DefenseOutcome.Hit ? strike.HealthDamage : 0;
            int spent = outcome == DefenseOutcome.Blocked || outcome == DefenseOutcome.GuardBroken
                ? Math.Min(Guard, strike.GuardCost) : 0;
            Guard -= spent;
            Health = damage >= Health ? 0 : Health - damage;
            ApplyImpactState(outcome, state, recoveryEndsUs);
            return new DefenseResolution(strike.Id, combatTimeUs, outcome, damage, spent, Health, Guard);
        }

        public void AdvanceTo(long combatTimeUs)
        {
            ValidateTime(combatTimeUs);
            DodgeCharges = ChargesAt(combatTimeUs);
            var state = StateAt(combatTimeUs);
            if (State == PlayerCombatState.Parrying && state == PlayerCombatState.Recovery)
            {
                _armedStrikeId = 0;
                ActionEndsUs = _parryRecoveryEndsUs;
            }
            if (State == PlayerCombatState.Staggered && state == PlayerCombatState.Recovery)
                ActionEndsUs = _hitRecoveryEndsUs;
            State = state;
            TimeUs = combatTimeUs;
            if (State == PlayerCombatState.Ready)
            {
                _armedStrikeId = 0;
                _hitRecoveryEndsUs = 0;
                _offenseImpactUs = 0;
                _dodgeAction = false;
                DodgeSide = DodgeSide.None;
                ActionEndsUs = 0;
            }
        }

        /// <summary>Suspension clears held defenses while retaining resources and committed action timers.</summary>
        public void ReleaseHeldDefense()
        {
            GuardHeld = false;
            _armedStrikeId = 0;
            if (State == PlayerCombatState.Guarding)
                State = PlayerCombatState.Ready;
        }

        private void BeginDodge(DefenseCommandKind kind, long timeUs, long endsUs)
        {
            GuardHeld = false;
            DodgeCharges--;
            _chargesAfterDodge = DodgeCharges;
            _rechargeOriginUs = timeUs;
            _hasRechargeOrigin = true;
            DodgeStartedUs = timeUs;
            DodgeSide = kind == DefenseCommandKind.DodgeLeft ? DodgeSide.Left : DodgeSide.Right;
            _dodgeAction = true;
            ActionEndsUs = endsUs;
            State = PlayerCombatState.Dodging;
        }

        private DefenseOutcome OutcomeAt(EnemyStrike strike, long timeUs, PlayerCombatState state)
        {
            if (state == PlayerCombatState.Dead) return DefenseOutcome.IgnoredAfterDeath;
            if (state == PlayerCombatState.Parrying && _armedStrikeId == strike.Id &&
                _armedImpactUs == timeUs && _armedDirection == strike.RequiredParryDirection &&
                (strike.AllowedDefenses & DefenseMask.Parry) != 0)
                return DefenseOutcome.Parried;
            if (_dodgeAction && state == PlayerCombatState.Dodging &&
                timeUs - DodgeStartedUs >= Tuning.DodgeAvoidanceStartUs &&
                timeUs - DodgeStartedUs <= Tuning.DodgeAvoidanceEndUs &&
                (strike.AllowedDefenses & DefenseMask.Dodge) != 0 &&
                (strike.SafeDodgeSides & DodgeSide) != 0)
                return DefenseOutcome.Dodged;
            if (state == PlayerCombatState.Guarding && GuardHeld && Guard > 0 &&
                (strike.AllowedDefenses & DefenseMask.Guard) != 0)
                return strike.GuardCost >= Guard ? DefenseOutcome.GuardBroken : DefenseOutcome.Blocked;
            return DefenseOutcome.Hit;
        }

        private long ImpactRecoveryEndsAt(EnemyStrike strike, long timeUs, DefenseOutcome outcome)
        {
            if (outcome == DefenseOutcome.GuardBroken) return checked(timeUs + Tuning.GuardStaggerUs);
            if (outcome == DefenseOutcome.Parried) return _parryRecoveryEndsUs;
            if (outcome == DefenseOutcome.Hit && strike.HealthDamage < Health)
                return checked(timeUs + Tuning.HitRecoveryUs);
            return ActionEndsUs;
        }

        private void ApplyImpactState(DefenseOutcome outcome, PlayerCombatState previousState, long endsUs)
        {
            if (outcome == DefenseOutcome.IgnoredAfterDeath || outcome == DefenseOutcome.Dodged ||
                outcome == DefenseOutcome.Blocked) return;
            GuardHeld = false;
            _armedStrikeId = 0;
            _dodgeAction = false;
            DodgeSide = DodgeSide.None;
            if (outcome == DefenseOutcome.Hit && previousState == PlayerCombatState.Staggered)
                _hitRecoveryEndsUs = endsUs;
            else
                ActionEndsUs = endsUs;
            if (Health == 0)
            {
                State = PlayerCombatState.Dead;
                ActionEndsUs = 0;
                _hitRecoveryEndsUs = 0;
            }
            else if (outcome == DefenseOutcome.GuardBroken || previousState == PlayerCombatState.Staggered)
                State = PlayerCombatState.Staggered;
            else
                State = PlayerCombatState.Recovery;
        }

        private PlayerCombatState StateAt(long timeUs)
        {
            if (State == PlayerCombatState.Dead || State == PlayerCombatState.Ready ||
                State == PlayerCombatState.Guarding) return State;
            if (State == PlayerCombatState.Parrying)
            {
                if (timeUs <= _armedImpactUs) return PlayerCombatState.Parrying;
                return timeUs < _parryRecoveryEndsUs ? PlayerCombatState.Recovery : PlayerCombatState.Ready;
            }
            if (State == PlayerCombatState.Staggered)
            {
                if (timeUs < ActionEndsUs) return PlayerCombatState.Staggered;
                return timeUs < _hitRecoveryEndsUs ? PlayerCombatState.Recovery : PlayerCombatState.Ready;
            }
            if (timeUs >= ActionEndsUs) return PlayerCombatState.Ready;
            if (State == PlayerCombatState.Attacking && timeUs >= _offenseImpactUs)
                return PlayerCombatState.Recovery;
            if (_dodgeAction && timeUs - DodgeStartedUs > Tuning.DodgeAvoidanceEndUs)
                return PlayerCombatState.Recovery;
            return State;
        }

        private int ChargesAt(long timeUs)
        {
            if (State == PlayerCombatState.Dead || !_hasRechargeOrigin) return DodgeCharges;
            long restored = (timeUs - _rechargeOriginUs) / Tuning.DodgeRechargeUs;
            int missing = Tuning.MaximumDodgeCharges - _chargesAfterDodge;
            return restored >= missing ? Tuning.MaximumDodgeCharges : _chargesAfterDodge + (int)restored;
        }

        private void ValidateTime(long timeUs)
        {
            if (timeUs < 0 || timeUs < TimeUs) throw new ArgumentOutOfRangeException(nameof(timeUs));
        }

        private static void ValidateCommand(DefenseCommand command)
        {
            if (command.Sequence <= 0 || command.InputTimestampUs < 0 || command.GuardActionId < 0 ||
                !Enum.IsDefined(typeof(DefenseCommandKind), command.Kind))
                throw new ArgumentOutOfRangeException(nameof(command));
        }
    }
}
