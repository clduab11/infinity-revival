using System;
using UnityEngine;

namespace Praxen.Game.Content.Definitions
{
    [CreateAssetMenu(menuName = "Praxen/Content/Combat Camera")]
    public sealed class CameraProfileDefinition : ContentDefinition
    {
        public bool EnableOrbit = true;
        public float OrbitDegrees = 3, FocusDistance = 8;
        public bool ReducedMotion;

        public CameraProfileSnapshot ToSnapshot()
        { Validate().ThrowIfInvalid(); return new CameraProfileSnapshot(this); }
        internal override void ValidateFields(ContentValidationContext context)
        {
            context.Require(Finite(OrbitDegrees) && OrbitDegrees >= 0 && OrbitDegrees <= 4, this,
                "OrbitDegrees", "Orbit must be finite and within zero to four degrees.");
            context.Require(Finite(FocusDistance) && FocusDistance >= 1 && FocusDistance <= 12, this,
                "FocusDistance", "Focus distance must be finite and within one to twelve units.");
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public sealed class CameraProfileSnapshot
    {
        public string Id { get; }
        public int Version { get; }
        public string LocalizationKey { get; }
        public bool EnableOrbit { get; }
        public float OrbitDegrees { get; }
        public float FocusDistance { get; }
        public bool ReducedMotion { get; }
        internal CameraProfileSnapshot(CameraProfileDefinition definition)
        {
            Id = definition.Id; Version = definition.Version; LocalizationKey = definition.LocalizationKey;
            EnableOrbit = definition.EnableOrbit; OrbitDegrees = definition.OrbitDegrees;
            FocusDistance = definition.FocusDistance; ReducedMotion = definition.ReducedMotion;
        }
    }
}
