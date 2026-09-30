using System;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Domain.Combat
{
    public enum AttackRequestStatus { Started, Buffered, Rejected }

    public readonly struct PlayerAttack
    {
        public long Id { get; }
        public InteractionPhase Phase { get; }
        public SwipeDirection Direction { get; }
        public long StartedUs { get; }
        public long ImpactUs { get; }
        public long RecoveryEndUs { get; }
        public int Damage { get; }
        public bool ComboFinisher { get; }

        public PlayerAttack(long id, InteractionPhase phase, SwipeDirection direction,
            long startedUs, long impactUs, long recoveryEndUs, int damage, bool comboFinisher)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (phase.Id <= 0 || phase.Kind != InteractionPhaseKind.PlayerOpening)
                throw new ArgumentOutOfRangeException(nameof(phase));
            if (!Enum.IsDefined(typeof(SwipeDirection), direction))
                throw new ArgumentOutOfRangeException(nameof(direction));
            if (startedUs < 0) throw new ArgumentOutOfRangeException(nameof(startedUs));
            if (impactUs <= startedUs) throw new ArgumentOutOfRangeException(nameof(impactUs));
            if (recoveryEndUs <= impactUs) throw new ArgumentOutOfRangeException(nameof(recoveryEndUs));
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            Id = id;
            Phase = phase;
            Direction = direction;
            StartedUs = startedUs;
            ImpactUs = impactUs;
            RecoveryEndUs = recoveryEndUs;
            Damage = damage;
            ComboFinisher = comboFinisher;
        }
    }

    public readonly struct AttackRequestResult
    {
        public AttackRequestStatus Status { get; }
        public PlayerAttack? Attack { get; }

        public AttackRequestResult(AttackRequestStatus status, PlayerAttack? attack = null)
        {
            if (!Enum.IsDefined(typeof(AttackRequestStatus), status) ||
                (status == AttackRequestStatus.Started) != attack.HasValue)
                throw new ArgumentOutOfRangeException(nameof(status));
            Status = status;
            Attack = attack;
        }
    }

    public readonly struct PlayerAttackResolution
    {
        public PlayerAttack Attack { get; }
        public long TimeUs { get; }
        public int Damage { get; }
        public int EnemyHealth { get; }

        public PlayerAttackResolution(PlayerAttack attack, long timeUs, int damage, int enemyHealth)
        {
            if (attack.Id <= 0) throw new ArgumentOutOfRangeException(nameof(attack));
            if (timeUs < 0) throw new ArgumentOutOfRangeException(nameof(timeUs));
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (enemyHealth < 0) throw new ArgumentOutOfRangeException(nameof(enemyHealth));
            Attack = attack;
            TimeUs = timeUs;
            Damage = damage;
            EnemyHealth = enemyHealth;
        }
    }
}
