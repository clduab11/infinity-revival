using System;
using UnityEngine;

namespace Praxen.Game.Content.Definitions
{
    [CreateAssetMenu(menuName = "Praxen/Content/Weapon Motion Profile")]
    public sealed class MotionProfileDefinition : ContentDefinition
    {
        public GameObject ActorPrefab;
        public string RigId;
        public bool RightHanded = true;
        public string WeaponTipPath, WeaponSocketPath, ShieldSocketPath;
        public string[] WeaponRendererPaths = Array.Empty<string>();
        public MotionBinding[] Bindings = Array.Empty<MotionBinding>();

        public MotionProfileSnapshot ToSnapshot()
        { Validate().ThrowIfInvalid(); return new MotionProfileSnapshot(this); }
        internal override void ValidateFields(ContentValidationContext context)
            => MotionProfileValidation.Validate(this, context);
    }
}
