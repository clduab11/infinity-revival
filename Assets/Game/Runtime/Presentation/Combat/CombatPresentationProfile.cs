using System;
using UnityEngine;

namespace Praxen.Game.Presentation.Combat
{
    [Serializable]
    public struct MotionClipBinding
    {
        public string Key;
        public AnimationClip Clip;
        public double ContactSeconds;
    }

    [Serializable]
    public struct CombatSoundBinding
    {
        public CombatCueKind Kind;
        public AudioClip Clip;
    }

    [CreateAssetMenu(menuName = "Praxen/Combat Presentation Profile")]
    public sealed class CombatPresentationProfile : ScriptableObject
    {
        public GameObject Soldier;
        public GameObject Captain;
        public string SoldierWeaponTipPath;
        public string CaptainWeaponTipPath;
        public MotionClipBinding[] SoldierMotions = Array.Empty<MotionClipBinding>();
        public MotionClipBinding[] CaptainMotions = Array.Empty<MotionClipBinding>();
        public CombatSoundBinding[] Sounds = Array.Empty<CombatSoundBinding>();
        [Header("Combat camera")]
        public bool EnableCameraOrbit = true;
        [Range(0, 4)] public float CameraOrbitDegrees = 3f;
        [Range(1, 12)] public float CameraFocusDistance = 8f;
        public bool ReducedMotion;

        public AnimationClip FindMotion(bool enemy, string key)
        {
            var motions = enemy ? CaptainMotions : SoldierMotions;
            if (motions == null) return null;
            AnimationClip idle = null;
            foreach (var binding in motions)
            {
                if (binding.Clip == null) continue;
                if (string.Equals(binding.Key, key, StringComparison.Ordinal)) return binding.Clip;
                if (string.Equals(binding.Key, "Idle", StringComparison.Ordinal)) idle = binding.Clip;
            }
            return idle;
        }

        public AudioClip FindSound(CombatCueKind kind)
        {
            if (Sounds == null) return null;
            foreach (var binding in Sounds)
                if (binding.Kind == kind && binding.Clip != null) return binding.Clip;
            return null;
        }
    }
}
