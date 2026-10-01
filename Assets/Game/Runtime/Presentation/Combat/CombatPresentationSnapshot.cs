using System;
using UnityEngine;

namespace Praxen.Game.Presentation.Combat
{
    /// <summary>Copies authoring records once; actor graphs never reread the authoring asset.</summary>
    internal sealed class CombatPresentationSnapshot
    {
        public readonly GameObject Soldier, Captain;
        public readonly string SoldierWeaponTipPath, CaptainWeaponTipPath;
        public readonly MotionClipBinding[] SoldierMotions, CaptainMotions;
        private readonly CombatSoundBinding[] sounds;
        public readonly bool EnableCameraOrbit, ReducedMotion;
        public readonly float CameraOrbitDegrees, CameraFocusDistance;

        public CombatPresentationSnapshot(CombatPresentationProfile source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.Soldier == null || source.Captain == null)
                throw new InvalidOperationException("Both combat actor prefabs are required.");
            if (!Finite(source.CameraOrbitDegrees) || !Finite(source.CameraFocusDistance))
                throw new InvalidOperationException("Combat camera settings must be finite.");
            Soldier = source.Soldier;
            Captain = source.Captain;
            SoldierWeaponTipPath = source.SoldierWeaponTipPath;
            CaptainWeaponTipPath = source.CaptainWeaponTipPath;
            SoldierMotions = Copy(source.SoldierMotions);
            CaptainMotions = Copy(source.CaptainMotions);
            sounds = Copy(source.Sounds);
            EnableCameraOrbit = source.EnableCameraOrbit;
            CameraOrbitDegrees = Mathf.Clamp(source.CameraOrbitDegrees, 0, 4);
            CameraFocusDistance = Mathf.Clamp(source.CameraFocusDistance, 1, 12);
            ReducedMotion = source.ReducedMotion;
        }

        public AudioClip FindSound(CombatCueKind kind)
        {
            foreach (var binding in sounds)
                if (binding.Kind == kind && binding.Clip != null) return binding.Clip;
            return null;
        }

        private static T[] Copy<T>(T[] values) => values == null ? Array.Empty<T>() : (T[])values.Clone();
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
