using System;
using System.Collections.Generic;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Domain.Content
{
    public sealed class WeaponSnapshot
    {
        public string Id { get; }
        public int Version { get; }
        public string LocalizationKey { get; }
        public WeaponFamily Family { get; }
        public bool OneHanded { get; }
        public bool ShieldCompatible { get; }
        public string MotionProfileId { get; }
        public IReadOnlyList<AttackSnapshot> Attacks { get; }

        public WeaponSnapshot(string id, int version, string localizationKey, WeaponFamily family,
            bool oneHanded, bool shieldCompatible, string motionProfileId,
            IReadOnlyList<AttackSnapshot> attacks)
        {
            ContentIdentity.Validate(id, version, localizationKey);
            ContentIdentity.Validate(motionProfileId, version, localizationKey);
            if (!Enum.IsDefined(typeof(WeaponFamily), family)) throw new ArgumentOutOfRangeException(nameof(family));
            if (!oneHanded || !shieldCompatible)
                throw new ArgumentException("Launch weapons require one hand and shield compatibility.");
            if (attacks == null || attacks.Count != 4)
                throw new ArgumentException("A weapon requires four cardinal attacks.", nameof(attacks));
            var copy = CopyAttacks(attacks);
            Id = id; Version = version; LocalizationKey = localizationKey; Family = family;
            OneHanded = oneHanded; ShieldCompatible = shieldCompatible; MotionProfileId = motionProfileId;
            Attacks = Array.AsReadOnly(copy);
        }

        public CombatOffenseTuning ToOffenseTuning() => new CombatOffenseTuning(
            attackDamage: Attacks[0].Damage, windupUs: Attacks[0].WindupUs,
            recoveryUs: Attacks[0].RecoveryUs, comboBonusPercent: Attacks[0].ComboDamageBonus);

        private static AttackSnapshot[] CopyAttacks(IReadOnlyList<AttackSnapshot> attacks)
        {
            var copy = new AttackSnapshot[attacks.Count];
            var directions = new HashSet<SwipeDirection>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < copy.Length; i++)
            {
                var attack = attacks[i] ?? throw new ArgumentException("An attack cannot be null.", nameof(attacks));
                if (!directions.Add(attack.Direction) || !ids.Add(attack.Id))
                    throw new ArgumentException("Attack IDs and directions must be unique.", nameof(attacks));
                if (i > 0 && (attack.WindupUs != copy[0].WindupUs || attack.RecoveryUs != copy[0].RecoveryUs ||
                    attack.Damage != copy[0].Damage || attack.ComboDamageBonus != copy[0].ComboDamageBonus))
                    throw new ArgumentException("The resolver does not support directional tuning overrides.", nameof(attacks));
                copy[i] = attack;
            }
            return copy;
        }
    }
}
