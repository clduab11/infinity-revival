using System;
using System.Collections.Generic;
using System.IO;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Content;
using Praxen.Game.Domain.Input;
using UnityEngine;

namespace Praxen.Game.Content.Definitions
{
    [CreateAssetMenu(menuName = "Praxen/Content/Weapon")]
    public sealed class WeaponDefinition : ContentDefinition
    {
        public WeaponFamily Family;
        public bool OneHanded = true, ShieldCompatible = true;
        public AttackDefinition[] Attacks = Array.Empty<AttackDefinition>();
        public MotionProfileDefinition MotionProfile;
        public Sprite Icon;
        public Material[] Materials = Array.Empty<Material>();
        public string ProvenancePath;
        public bool ProductionReady;
        public string[] RequestedCapabilities = Array.Empty<string>();

        public WeaponSnapshot ToRuntime()
        { Validate().ThrowIfInvalid(); return BuildRuntime(); }
        public CombatOffenseTuning ToOffenseTuning() => ToRuntime().ToOffenseTuning();
        internal WeaponSnapshot BuildRuntime()
        {
            var attacks = new AttackSnapshot[Attacks.Length];
            for (int i = 0; i < attacks.Length; i++) attacks[i] = Attacks[i].BuildRuntime();
            return new WeaponSnapshot(Id, Version, LocalizationKey, Family, OneHanded,
                ShieldCompatible, MotionProfile.Id, attacks);
        }

        internal override void ValidateFields(ContentValidationContext context)
        {
            context.Require(Enum.IsDefined(typeof(WeaponFamily), Family), this, "Family", "Only Sword, Axe, and Mace are supported.");
            context.Require(OneHanded, this, "OneHanded", "Two-handed launch weapons are unsupported.");
            context.Require(ShieldCompatible, this, "ShieldCompatible", "Launch weapons must retain the shield.");
            context.Require(RequestedCapabilities != null && RequestedCapabilities.Length == 0, this,
                "RequestedCapabilities", "No optional future gameplay capability is currently supported.");
            context.Child(this, "MotionProfile", MotionProfile);
            context.Require(!ProductionReady || Icon != null, this, "Icon", "Production-ready weapons require an icon.");
            context.Require(Materials != null && Materials.Length > 0, this, "Materials", "Assign weapon materials.");
            if (Materials != null)
                foreach (var material in Materials) context.Require(material != null, this, "Materials", "Repair missing material references.");
            context.Text(this, "ProvenancePath", ProvenancePath);
            ValidateProvenance(context);
            ValidateAttacks(context);
        }

        private void ValidateProvenance(ContentValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(ProvenancePath)) return;
            bool canonical = !Path.IsPathRooted(ProvenancePath) && ProvenancePath.IndexOf(':') < 0 &&
                ProvenancePath.IndexOf('\\') < 0;
            foreach (string segment in ProvenancePath.Split('/'))
                canonical &= segment.Length > 0 && segment != "." && segment != "..";
            context.Require(canonical, this, "ProvenancePath", "Use a canonical project-relative provenance path.");
            if (!canonical) return;
#if UNITY_EDITOR
            context.Attempt(this, "ProvenancePath", () =>
            {
                string path = Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath), ProvenancePath);
                context.Require(File.Exists(path), this, "ProvenancePath", "The provenance file reference is broken.");
            });
#endif
        }

        private void ValidateAttacks(ContentValidationContext context)
        {
            context.Require(Attacks != null && Attacks.Length == 4, this, "Attacks", "Bind four cardinal attacks.");
            if (Attacks == null) return;
            var directions = new HashSet<SwipeDirection>();
            AttackDefinition baseline = null;
            foreach (var attack in Attacks)
            {
                context.Child(this, "Attacks", attack);
                if (attack == null) continue;
                context.Require(directions.Add(attack.Direction), this, "Attacks", "Cardinal direction bindings must be unique.");
                if (baseline == null) baseline = attack;
                else context.Require(attack.WindupUs == baseline.WindupUs && attack.RecoveryUs == baseline.RecoveryUs &&
                    attack.Damage == baseline.Damage && attack.ComboDamageBonus == baseline.ComboDamageBonus,
                    this, "Attacks", "Directional tuning overrides are unsupported by the current resolver.");
                ValidateMotionBinding(context, attack);
            }
        }

        private void ValidateMotionBinding(ContentValidationContext context, AttackDefinition attack)
        {
            if (MotionProfile == null || MotionProfile.Bindings == null) return;
            foreach (var binding in MotionProfile.Bindings)
                if (string.Equals(binding.Key, attack.MotionKey, StringComparison.Ordinal))
                    return;
            context.Add(this, "Attacks", "The attack's presentation motion key has no binding.");
        }
    }
}
