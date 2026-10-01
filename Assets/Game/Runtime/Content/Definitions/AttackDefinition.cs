using System;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Content;
using Praxen.Game.Domain.Input;
using UnityEngine;

namespace Praxen.Game.Content.Definitions
{
    [CreateAssetMenu(menuName = "Praxen/Content/Player Attack")]
    public sealed class AttackDefinition : ContentDefinition
    {
        public SwipeDirection Direction;
        public long WindupUs = 100000;
        public long RecoveryUs = 400000;
        public long VisualReleaseUs = 60000;
        public int Damage = 20;
        public int ComboDamageBonus = 10;
        public string MotionKey;

        public AttackSnapshot ToRuntime()
        { Validate().ThrowIfInvalid(); return BuildRuntime(); }
        internal AttackSnapshot BuildRuntime() => new AttackSnapshot(Id, Version, LocalizationKey,
            Direction, WindupUs, RecoveryUs, VisualReleaseUs, Damage, ComboDamageBonus, MotionKey);

        internal override void ValidateFields(ContentValidationContext context)
        {
            context.Require(Enum.IsDefined(typeof(SwipeDirection), Direction), this, "Direction", "Select a cardinal direction.");
            context.Require(WindupUs > 0, this, "WindupUs", "Logical windup must be positive.");
            context.Require(RecoveryUs > 0, this, "RecoveryUs", "Logical recovery must be positive.");
            context.Require(VisualReleaseUs >= 0 && VisualReleaseUs < WindupUs, this, "VisualReleaseUs",
                "The visual release must be within the pre-contact windup.");
            context.Require(string.Equals(MotionKey, "Cut" + Direction, StringComparison.Ordinal), this,
                "MotionKey", "Bind the captured direction to its matching Cut motion key.");
            context.Attempt(this, "Timing", () => new CombatOffenseTuning(windupUs: WindupUs,
                recoveryUs: RecoveryUs, attackDamage: Damage, comboBonusPercent: ComboDamageBonus));
        }
    }
}
