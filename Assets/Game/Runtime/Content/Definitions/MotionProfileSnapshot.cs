using System;
using System.Collections.Generic;
using UnityEngine;

namespace Praxen.Game.Content.Definitions
{
    [Serializable]
    public struct MotionBinding
    {
        public string Key;
        public AnimationClip Clip;
        public double ContactSeconds, DurationSeconds, ReleaseSeconds;
        public bool Loop;
    }

    /// <summary>Frozen authoring values and Unity handles; clips never supply gameplay timing.</summary>
    public sealed class MotionProfileSnapshot
    {
        public string Id { get; }
        public int Version { get; }
        public string LocalizationKey { get; }
        public GameObject ActorPrefab { get; }
        public string RigId { get; }
        public bool RightHanded { get; }
        public string WeaponTipPath { get; }
        public string WeaponSocketPath { get; }
        public string ShieldSocketPath { get; }
        public IReadOnlyList<string> WeaponRendererPaths { get; }
        public IReadOnlyList<MotionBinding> Bindings { get; }

        internal MotionProfileSnapshot(MotionProfileDefinition definition)
        {
            Id = definition.Id; Version = definition.Version; LocalizationKey = definition.LocalizationKey;
            ActorPrefab = definition.ActorPrefab; RigId = definition.RigId; RightHanded = definition.RightHanded;
            WeaponTipPath = definition.WeaponTipPath; WeaponSocketPath = definition.WeaponSocketPath;
            ShieldSocketPath = definition.ShieldSocketPath;
            WeaponRendererPaths = Array.AsReadOnly((string[])definition.WeaponRendererPaths.Clone());
            Bindings = Array.AsReadOnly((MotionBinding[])definition.Bindings.Clone());
        }
    }
}
