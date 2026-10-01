using System;
using System.Linq;
using Praxen.Game.Content.Definitions;
using UnityEngine;

namespace Praxen.Game.Presentation.Combat
{
    /// <summary>Copies admitted authoring values at a deliberate encounter boundary.</summary>
    public static class EncounterPresentationCapture
    {
        public static CombatPresentationProfile Capture(CombatPresentationProfile source,
            WeaponDefinition weapon, MotionProfileDefinition enemy, CameraProfileDefinition camera)
        {
            if (source == null) throw new InvalidOperationException("An encounter requires a presentation profile.");
            var playerMotion = weapon == null ? null : weapon.MotionProfile.ToSnapshot();
            var enemyMotion = weapon == null ? null : RequiredEnemy(enemy).ToSnapshot();
            var cameraSettings = camera == null ? null : camera.ToSnapshot();
            var copy = ScriptableObject.CreateInstance<CombatPresentationProfile>();
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.Soldier = playerMotion?.ActorPrefab ?? source.Soldier;
            copy.Captain = enemyMotion?.ActorPrefab ?? source.Captain;
            copy.SoldierMotions = playerMotion == null ? Clone(source.SoldierMotions) : Bindings(playerMotion);
            copy.CaptainMotions = enemyMotion == null ? Clone(source.CaptainMotions) : Bindings(enemyMotion);
            copy.SoldierWeaponTipPath = playerMotion?.WeaponTipPath ?? source.SoldierWeaponTipPath;
            copy.CaptainWeaponTipPath = enemyMotion?.WeaponTipPath ?? source.CaptainWeaponTipPath;
            copy.Sounds = source.Sounds == null ? Array.Empty<CombatSoundBinding>() :
                (CombatSoundBinding[])source.Sounds.Clone();
            copy.EnableCameraOrbit = cameraSettings?.EnableOrbit ?? source.EnableCameraOrbit;
            copy.CameraOrbitDegrees = cameraSettings?.OrbitDegrees ?? source.CameraOrbitDegrees;
            copy.CameraFocusDistance = cameraSettings?.FocusDistance ?? source.CameraFocusDistance;
            copy.ReducedMotion = cameraSettings?.ReducedMotion ?? source.ReducedMotion;
            return copy;
        }

        private static MotionProfileDefinition RequiredEnemy(MotionProfileDefinition profile)
        {
            if (profile == null) throw new InvalidOperationException("Authored content requires the enemy motion profile.");
            profile.Validate().ThrowIfInvalid();
            return profile;
        }

        private static MotionClipBinding[] Clone(MotionClipBinding[] source) => source == null
            ? Array.Empty<MotionClipBinding>() : (MotionClipBinding[])source.Clone();

        private static MotionClipBinding[] Bindings(MotionProfileSnapshot profile) =>
            profile.Bindings.Select(binding => new MotionClipBinding
            { Key = binding.Key, Clip = binding.Clip, ContactSeconds = binding.ContactSeconds }).ToArray();
    }
}
