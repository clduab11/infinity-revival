using System;
using System.Collections.Generic;
using UnityEngine;

namespace Praxen.Game.Content.Definitions
{
    internal static class MotionProfileValidation
    {
        private static readonly string[] RequiredKeys = { "Idle", "Guard", "Parry", "DodgeLeft", "DodgeRight",
            "CutUp", "CutDown", "CutLeft", "CutRight", "Hit", "Death", "EnemyTell", "EnemyAttack" };

        internal static void Validate(MotionProfileDefinition profile, ContentValidationContext context)
        {
            context.Text(profile, "RigId", profile.RigId);
            context.Require(profile.RightHanded, profile, "RightHanded", "Launch profiles require the right-hand weapon socket.");
            ValidateActor(profile, context);
            ValidateBindings(profile, context);
        }

        private static void ValidateActor(MotionProfileDefinition profile, ContentValidationContext context)
        {
            if (profile.ActorPrefab == null)
            { context.Add(profile, "ActorPrefab", "Assign the actor prefab."); return; }
            var animators = profile.ActorPrefab.GetComponentsInChildren<Animator>(true);
            context.Require(animators.Length == 1, profile, "ActorPrefab", "The actor must have exactly one LOD0 Animator.");
            var animator = animators.Length == 1 ? animators[0] : null;
            context.Require(animator != null && animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman,
                profile, "ActorPrefab", "Use a valid Humanoid avatar.");
            context.Require(animator == null || !animator.applyRootMotion, profile, "ActorPrefab", "Disable Animator root motion.");
            foreach (var transform in profile.ActorPrefab.GetComponentsInChildren<Transform>(true))
                context.Require(Positive(transform.localScale), profile, "ActorPrefab", "Every local scale must be finite and positive.");
            ValidateMarker(profile, context, animator, "WeaponTipPath", profile.WeaponTipPath, "WeaponTip");
            ValidateMarker(profile, context, animator, "WeaponSocketPath", profile.WeaponSocketPath, "WeaponSocket");
            ValidateMarker(profile, context, animator, "ShieldSocketPath", profile.ShieldSocketPath, "ShieldSocket");
            ValidateRenderers(profile, context);
        }

        private static void ValidateMarker(MotionProfileDefinition profile, ContentValidationContext context,
            Animator animator, string field, string path, string role)
        {
            var node = Resolve(profile.ActorPrefab.transform, path);
            context.Require(node != null && node.name == role && animator != null && node.IsChildOf(animator.transform),
                profile, field, "Use the exact unambiguous root-relative " + role + " path under the sole LOD0 Animator.");
        }

        private static void ValidateRenderers(MotionProfileDefinition profile, ContentValidationContext context)
        {
            context.Require(profile.WeaponRendererPaths != null && profile.WeaponRendererPaths.Length == 3,
                profile, "WeaponRendererPaths", "Bind one weapon renderer for each of the three LODs.");
            if (profile.WeaponRendererPaths == null) return;
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in profile.WeaponRendererPaths)
            {
                var node = Resolve(profile.ActorPrefab.transform, path);
                var renderer = node == null ? null : node.GetComponent<Renderer>();
                context.Require(path != null && paths.Add(path) && renderer != null, profile,
                    "WeaponRendererPaths", "Each renderer needs a unique exact root-relative path.");
                if (renderer == null) continue;
                context.Require(Finite(renderer.bounds.center) && Positive(renderer.bounds.size), profile,
                    "WeaponRendererPaths", "Weapon renderer bounds must be finite and positive.");
                foreach (var material in renderer.sharedMaterials)
                    context.Require(material != null, profile, "WeaponRendererPaths", "Repair missing weapon materials.");
            }
        }

        private static void ValidateBindings(MotionProfileDefinition profile, ContentValidationContext context)
        {
            context.Require(profile.Bindings != null && profile.Bindings.Length == RequiredKeys.Length,
                profile, "Bindings", "Supply exactly thirteen required motion bindings.");
            if (profile.Bindings == null) return;
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < profile.Bindings.Length; i++)
            {
                var binding = profile.Bindings[i];
                string field = "Bindings[" + i + "]";
                bool known = Array.IndexOf(RequiredKeys, binding.Key) >= 0;
                context.Require(known && keys.Add(binding.Key), profile, field + ".Key", "Use one unique required motion key.");
                ValidateClip(profile, context, binding, field);
            }
            foreach (string key in RequiredKeys)
                context.Require(keys.Contains(key), profile, "Bindings", "Missing required motion key: " + key + ".");
        }

        private static void ValidateClip(MotionProfileDefinition profile, ContentValidationContext context,
            MotionBinding binding, string field)
        {
            bool looping = binding.Key == "Idle" || binding.Key == "Guard";
            context.Require(binding.Loop == looping, profile, field + ".Loop", "Only Idle and Guard may loop.");
            context.Require(Finite(binding.DurationSeconds) && binding.DurationSeconds > 0, profile,
                field + ".DurationSeconds", "Clip duration must be finite and positive.");
            context.Require(Finite(binding.ContactSeconds) && binding.ContactSeconds >= 0 &&
                binding.ContactSeconds < binding.DurationSeconds, profile, field + ".ContactSeconds",
                "Contact must be finite and inside the clip duration.");
            bool attack = binding.Key != null && (binding.Key.StartsWith("Cut", StringComparison.Ordinal) || binding.Key == "EnemyAttack");
            context.Require(!attack || binding.ContactSeconds > 0, profile, field + ".ContactSeconds", "Attack contact must be positive.");
            context.Require(Finite(binding.ReleaseSeconds) && binding.ReleaseSeconds >= 0 &&
                (attack ? binding.ReleaseSeconds < binding.ContactSeconds : binding.ReleaseSeconds <= binding.ContactSeconds),
                profile, field + ".ReleaseSeconds", "Release must remain within the pre-contact interval.");
            if (binding.Clip == null)
            { context.Add(profile, field + ".Clip", "Assign the required motion clip."); return; }
            context.Require(binding.Clip.isHumanMotion, profile, field + ".Clip", "Use a Humanoid clip compatible with the declared rig.");
            context.Require(binding.Clip.events.Length == 0, profile, field + ".Clip", "Remove gameplay animation events.");
            ValidateRootPolicy(profile, context, binding, field);
            context.Require(binding.Clip.isLooping == looping, profile, field + ".Loop", "The imported clip loop policy must match its motion key.");
            context.Require(Math.Abs(binding.DurationSeconds - binding.Clip.length) <= 0.0001, profile,
                field + ".DurationSeconds", "Capture the imported clip's actual duration.");
        }

        private static void ValidateRootPolicy(MotionProfileDefinition profile, ContentValidationContext context,
            MotionBinding binding, string field)
        {
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(binding.Clip);
            var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.ModelImporter;
            UnityEditor.ModelImporterClipAnimation imported = null;
            if (importer != null)
                foreach (var clip in importer.clipAnimations)
                    if (string.Equals(clip.name, binding.Clip.name, StringComparison.Ordinal)) imported = clip;
            context.Require(imported != null && imported.lockRootPositionXZ && imported.lockRootHeightY &&
                imported.lockRootRotation, profile, field + ".Clip",
                "Import this exact clip with root position, height, and rotation baked into its bone pose.");
#endif
        }

        private static Transform Resolve(Transform root, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            foreach (string segment in path.Split('/'))
            {
                if (segment.Length == 0 || segment == "." || segment == "..") return null;
                Transform match = null;
                for (int i = 0; i < root.childCount; i++)
                    if (string.Equals(root.GetChild(i).name, segment, StringComparison.Ordinal))
                    {
                        if (match != null) return null;
                        match = root.GetChild(i);
                    }
                if (match == null) return null;
                root = match;
            }
            return root;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static bool Positive(Vector3 value) => Finite(value) && value.x > 0 && value.y > 0 && value.z > 0;
    }
}
