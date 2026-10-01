using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Praxen.Game.Editor
{
    /// <summary>Six original pilot files only; the accepted sword policy stays unchanged.</summary>
    public sealed class WeaponPilotImportPolicy : AssetPostprocessor
    {
        public const string Folder = "Assets/Game/Content/Characters/WeaponFamilyPilot/";
        private static readonly string[] Models = { "AxePilot_LOD0.fbx", "AxePilot_LOD1.fbx", "AxePilot_LOD2.fbx",
            "MacePilot_LOD0.fbx", "MacePilot_LOD1.fbx", "MacePilot_LOD2.fbx" };
        private static readonly string[] Bones = { "Hips", "Spine", "Chest", "Neck", "Head",
            "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
            "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
            "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes",
            "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes" };

        private bool IsAdmitted => Models.Any(name => assetPath == Folder + name);
        public override uint GetVersion() => 1;

        private void OnPreprocessModel()
        {
            if (!IsAdmitted) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1;
            importer.useFileUnits = true;
            importer.isReadable = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.skinWeights = ModelImporterSkinWeights.Custom;
            importer.maxBonesPerVertex = 4;
            importer.minBoneWeight = .001f;
            importer.optimizeGameObjects = false;
            importer.importAnimation = assetPath.EndsWith("_LOD0.fbx", StringComparison.Ordinal);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            var description = importer.humanDescription;
            description.human = Bones.Select(name => new HumanBone
                { boneName = name, humanName = name, limit = new HumanLimit { useDefaultValues = true } }).ToArray();
            description.upperArmTwist = description.lowerArmTwist = .5f;
            description.upperLegTwist = description.lowerLegTwist = .5f;
            description.armStretch = description.legStretch = .05f;
            importer.humanDescription = description;
        }

        private void OnPreprocessAnimation()
        {
            if (!IsAdmitted) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = clip.name.Split('|').Last();
                clip.loopTime = clip.name == "Idle" || clip.name == "Guard";
                clip.loopPose = clip.loopTime;
                clip.lockRootPositionXZ = clip.lockRootHeightY = clip.lockRootRotation = true;
                clip.keepOriginalPositionXZ = clip.keepOriginalPositionY = clip.keepOriginalOrientation = true;
                clip.events = Array.Empty<AnimationEvent>();
            }
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.clipAnimations = clips;
        }

        private void OnPostprocessModel(GameObject model)
        {
            if (!IsAdmitted) return;
            RestoreBindPose(model);
            foreach (var node in model.GetComponentsInChildren<Transform>())
            {
                if (node.name.EndsWith("_WeaponTip", StringComparison.Ordinal)) node.name = "WeaponTip";
                else if (node.name.EndsWith("_WeaponSocket", StringComparison.Ordinal)) node.name = "WeaponSocketMarker";
                else if (node.name.EndsWith("_ShieldSocket", StringComparison.Ordinal)) node.name = "ShieldSocketMarker";
            }
        }

        private static void RestoreBindPose(GameObject model)
        {
            var poses = model.GetComponentsInChildren<SkinnedMeshRenderer>()
                .SelectMany(renderer => renderer.bones.Select((bone, i) => new
                {
                    Bone = bone,
                    World = renderer.localToWorldMatrix * renderer.sharedMesh.bindposes[i].inverse
                })).GroupBy(p => p.Bone).Select(group => group.First())
                .OrderBy(p => p.Bone.GetComponentsInParent<Transform>().Length).ToArray();
            foreach (var pose in poses)
            {
                var local = pose.Bone.parent.worldToLocalMatrix * pose.World;
                pose.Bone.localPosition = local.GetColumn(3);
                pose.Bone.localRotation = local.rotation;
                pose.Bone.localScale = local.lossyScale;
            }
        }
    }
}
