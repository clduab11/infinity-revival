using System;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Domain.Content
{
    public sealed class AttackSnapshot
    {
        public string Id { get; }
        public int Version { get; }
        public string LocalizationKey { get; }
        public SwipeDirection Direction { get; }
        public long WindupUs { get; }
        public long RecoveryUs { get; }
        public long VisualReleaseUs { get; }
        public int Damage { get; }
        public int ComboDamageBonus { get; }
        public string MotionKey { get; }

        public AttackSnapshot(string id, int version, string localizationKey, SwipeDirection direction,
            long windupUs, long recoveryUs, long visualReleaseUs, int damage, int comboDamageBonus,
            string motionKey)
        {
            ContentIdentity.Validate(id, version, localizationKey);
            if (!Enum.IsDefined(typeof(SwipeDirection), direction))
                throw new ArgumentOutOfRangeException(nameof(direction));
            if (visualReleaseUs < 0 || visualReleaseUs >= windupUs)
                throw new ArgumentOutOfRangeException(nameof(visualReleaseUs));
            if (!string.Equals(motionKey, "Cut" + direction, StringComparison.Ordinal))
                throw new ArgumentException("The motion key must match the captured direction.", nameof(motionKey));
            _ = new CombatOffenseTuning(windupUs: windupUs, recoveryUs: recoveryUs,
                attackDamage: damage, comboBonusPercent: comboDamageBonus);
            Id = id; Version = version; LocalizationKey = localizationKey; Direction = direction;
            WindupUs = windupUs; RecoveryUs = recoveryUs; VisualReleaseUs = visualReleaseUs;
            Damage = damage; ComboDamageBonus = comboDamageBonus; MotionKey = motionKey;
        }
    }
}
