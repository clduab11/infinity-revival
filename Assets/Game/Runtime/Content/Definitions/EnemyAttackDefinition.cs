using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using UnityEngine;

namespace Praxen.Game.Content.Definitions
{
    [CreateAssetMenu(menuName = "Praxen/Content/Enemy Attack")]
    public sealed class EnemyAttackDefinition : ContentDefinition
    {
        public DefenseMask AllowedDefenses = DefenseMask.Guard | DefenseMask.Parry | DefenseMask.Dodge;
        public DodgeSide SafeDodgeSides = DodgeSide.Left;
        public SwipeDirection ParryDirection = SwipeDirection.Up;
        public int GuardCost = 20, Damage = 25;
        public long TelegraphDurationUs = 650000, RecoveryDurationUs = 500000;

        public EnemyPatternStep ToRuntime(long gapBeforeUs = 0)
        { Validate().ThrowIfInvalid(); return BuildRuntime(gapBeforeUs); }
        internal EnemyPatternStep BuildRuntime(long gapBeforeUs) => new EnemyPatternStep(Id,
            AllowedDefenses, SafeDodgeSides, ParryDirection, GuardCost, Damage,
            TelegraphDurationUs, RecoveryDurationUs, gapBeforeUs);
        internal override void ValidateFields(ContentValidationContext context)
        {
            context.Require(TelegraphDurationUs > 0, this, "TelegraphDurationUs", "Authoring telegraph must be positive.");
            context.Require(RecoveryDurationUs > 0, this, "RecoveryDurationUs", "Authoring recovery must be positive.");
            context.Attempt(this, "Timing", () => BuildRuntime(0));
        }
    }
}
