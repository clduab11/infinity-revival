using System;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Domain.Combat
{
    /// <summary>Phase-owned attacks, one pending swipe, and resolved-hit combo state.</summary>
    public sealed class OffenseCombatant
    {
        private GestureCommand? _buffer;
        private long _lastAttackId;
        private bool _impactResolved;
        private int _comboLength;
        private SwipeDirection _comboDirection;

        public DefenseCombatant Player { get; }
        public CombatOffenseTuning Tuning { get; }
        public long TimeUs { get; private set; }
        public int EnemyHealth { get; private set; }
        public InteractionPhase OpeningPhase { get; private set; } = InteractionPhase.Inactive;
        public long OpeningStartUs { get; private set; }
        public long OpeningEndUs { get; private set; }
        public bool IsOpening => OpeningPhase.Kind == InteractionPhaseKind.PlayerOpening &&
            TimeUs >= OpeningStartUs && TimeUs < OpeningEndUs && !IsTerminal;
        public bool HasBuffer => _buffer.HasValue;
        public long BufferedExpiresUs { get; private set; }
        public PlayerAttack? ActiveAttack { get; private set; }

        public OffenseCombatant(DefenseCombatant player, CombatOffenseTuning tuning = null)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Tuning = tuning ?? new CombatOffenseTuning();
            TimeUs = player.TimeUs;
            EnemyHealth = Tuning.EnemyHealth;
        }

        public void StartOpening(InteractionPhase phase, long startUs, long durationUs)
        {
            ValidateTime(startUs);
            if (phase.Id <= 0 || phase.Kind != InteractionPhaseKind.PlayerOpening)
                throw new ArgumentOutOfRangeException(nameof(phase));
            if (durationUs <= 0) throw new ArgumentOutOfRangeException(nameof(durationUs));
            long endUs = checked(startUs + durationUs);
            AdvanceTo(startUs);
            ClearTransient();
            OpeningPhase = IsTerminal ? InteractionPhase.Inactive : phase;
            OpeningStartUs = startUs;
            OpeningEndUs = endUs;
        }

        public void CloseOpening(long timeUs)
        {
            AdvanceTo(timeUs);
            ClosePhase();
        }

        /// <summary>Committed attacks and shared recovery survive suspension; pending swipes do not.</summary>
        public void Suspend() => ClearTransient();

        public void AdvanceTo(long timeUs)
        {
            ValidateTime(timeUs);
            Player.AdvanceTo(timeUs);
            TimeUs = timeUs;
            if (IsTerminal || (OpeningPhase.Kind == InteractionPhaseKind.PlayerOpening && timeUs >= OpeningEndUs))
                ClosePhase();
            else if (HasBuffer && timeUs >= BufferedExpiresUs)
                ClearBuffer();
        }

        public AttackRequestResult RequestAttack(GestureCommand command, long timeUs)
        {
            ValidateTime(timeUs);
            ValidateCommand(command);
            if (!Eligible(command, timeUs) || (HasBuffer && timeUs < BufferedExpiresUs))
            {
                AdvanceTo(timeUs);
                return Rejected();
            }
            var candidate = Candidate(command, timeUs);
            long expiresUs = checked(timeUs + Tuning.BufferUs);
            AdvanceTo(timeUs);
            if (CanStart)
                return Start(candidate);
            _buffer = command;
            BufferedExpiresUs = expiresUs;
            return new AttackRequestResult(AttackRequestStatus.Buffered);
        }

        public AttackRequestResult TryConsumeBuffer(long timeUs)
        {
            ValidateTime(timeUs);
            if (!_buffer.HasValue || timeUs >= BufferedExpiresUs || !Eligible(_buffer.Value, timeUs))
            {
                AdvanceTo(timeUs);
                ClearBuffer();
                return Rejected();
            }
            var candidate = Candidate(_buffer.Value, timeUs);
            AdvanceTo(timeUs);
            if (!CanStart) return Rejected();
            ClearBuffer();
            return Start(candidate);
        }

        public PlayerAttackResolution? ResolveImpact(long attackId, long timeUs)
        {
            ValidateTime(timeUs);
            AdvanceTo(timeUs);
            if (!ActiveAttack.HasValue || ActiveAttack.Value.Id != attackId || _impactResolved)
                return null;
            var attack = ActiveAttack.Value;
            if (timeUs != attack.ImpactUs || !IsOpening || !SamePhase(attack.Phase, OpeningPhase))
                return null;
            EnemyHealth = attack.Damage >= EnemyHealth ? 0 : EnemyHealth - attack.Damage;
            _impactResolved = true;
            ApplyCombo(attack);
            var result = new PlayerAttackResolution(attack, timeUs, attack.Damage, EnemyHealth);
            if (EnemyHealth == 0) ClosePhase();
            return result;
        }

        public void CompleteRecovery(long attackId, long timeUs)
        {
            AdvanceTo(timeUs);
            if (!ActiveAttack.HasValue || ActiveAttack.Value.Id != attackId ||
                timeUs < ActiveAttack.Value.RecoveryEndUs) return;
            ActiveAttack = null;
            _impactResolved = false;
        }

        private bool IsTerminal => Player.State == PlayerCombatState.Dead || EnemyHealth == 0;
        private bool CanStart => Player.State == PlayerCombatState.Ready || Player.State == PlayerCombatState.Guarding;

        private bool Eligible(GestureCommand command, long timeUs)
            => !IsTerminal && OpeningPhase.Kind == InteractionPhaseKind.PlayerOpening &&
                timeUs >= OpeningStartUs && timeUs < OpeningEndUs && command.Intent == GestureIntent.Attack &&
                SamePhase(command.Phase, OpeningPhase);

        private PlayerAttack Candidate(GestureCommand command, long timeUs)
        {
            long impactUs = checked(timeUs + Tuning.WindupUs);
            long recoveryEndUs = checked(impactUs + Tuning.RecoveryUs);
            long id = checked(_lastAttackId + 1);
            bool finisher = _comboLength == 2 && Opposite(_comboDirection, command.Direction);
            int damage = finisher ? checked((int)(Tuning.AttackDamage +
                (long)Tuning.AttackDamage * Tuning.ComboBonusPercent / 100)) : Tuning.AttackDamage;
            return new PlayerAttack(id, command.Phase, command.Direction, timeUs,
                impactUs, recoveryEndUs, damage, finisher);
        }

        private AttackRequestResult Start(PlayerAttack candidate)
        {
            if (candidate.ImpactUs >= OpeningEndUs ||
                !Player.TryBeginOffense(candidate.StartedUs, candidate.ImpactUs, candidate.RecoveryEndUs))
                return Rejected();
            _lastAttackId = candidate.Id;
            ActiveAttack = candidate;
            _impactResolved = false;
            return new AttackRequestResult(AttackRequestStatus.Started, candidate);
        }

        private void ApplyCombo(PlayerAttack attack)
        {
            if (attack.ComboFinisher)
                _comboLength = 0;
            else
                _comboLength = _comboLength > 0 && Opposite(_comboDirection, attack.Direction) ? _comboLength + 1 : 1;
            _comboDirection = attack.Direction;
        }

        private void ClosePhase()
        {
            OpeningPhase = InteractionPhase.Inactive;
            ActiveAttack = null;
            _impactResolved = false;
            ClearTransient();
        }

        private void ClearTransient()
        {
            ClearBuffer();
            _comboLength = 0;
        }

        private void ClearBuffer()
        {
            _buffer = null;
            BufferedExpiresUs = 0;
        }

        private void ValidateTime(long timeUs)
        {
            if (timeUs < 0 || timeUs < TimeUs || timeUs < Player.TimeUs)
                throw new ArgumentOutOfRangeException(nameof(timeUs));
        }

        private static void ValidateCommand(GestureCommand command)
        {
            if (command.Sequence <= 0 || command.InputTimestampUs < 0 || command.PointerId <= 0 ||
                !Enum.IsDefined(typeof(SwipeDirection), command.Direction) ||
                !Enum.IsDefined(typeof(GestureIntent), command.Intent) || command.Phase.Id <= 0 ||
                command.Phase.Kind == InteractionPhaseKind.Inactive ||
                !Enum.IsDefined(typeof(InteractionPhaseKind), command.Phase.Kind))
                throw new ArgumentOutOfRangeException(nameof(command));
        }

        private static bool SamePhase(InteractionPhase first, InteractionPhase second)
            => first.Id == second.Id && first.Kind == second.Kind;

        private static bool Opposite(SwipeDirection first, SwipeDirection second)
            => (first == SwipeDirection.Left && second == SwipeDirection.Right) ||
                (first == SwipeDirection.Right && second == SwipeDirection.Left) ||
                (first == SwipeDirection.Up && second == SwipeDirection.Down) ||
                (first == SwipeDirection.Down && second == SwipeDirection.Up);

        private static AttackRequestResult Rejected() => new AttackRequestResult(AttackRequestStatus.Rejected);
    }
}
