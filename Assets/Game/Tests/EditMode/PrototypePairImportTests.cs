using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class PrototypePairImportTests
    {
        private const string Folder = "Assets/Game/Content/Characters/PrototypePair/";
        private static readonly string[] Actions = { "Idle", "Guard", "Parry", "DodgeLeft", "DodgeRight",
            "CutUp", "CutDown", "CutLeft", "CutRight", "Hit", "Death", "EnemyTell", "EnemyAttack" };

        private static string Path(string actor, int lod = 0) => Folder + actor + "_LOD" + lod + ".fbx";
        private static GameObject Model(string actor, int lod = 0)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Path(actor, lod));
            Assert.That(model, Is.Not.Null, Path(actor, lod));
            return model;
        }
        private static AnimationClip Clip(string actor, string action)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(Path(actor)).OfType<AnimationClip>()
                .SingleOrDefault(c => c.name == action);
            Assert.That(clip, Is.Not.Null, actor + " " + action);
            return clip;
        }

        [TestCase("Soldier")]
        [TestCase("Captain")]
        public void ImportedPairHasValidHumanoidAvatars(string actor)
        {
            var animator = Model(actor).GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.avatar, Is.Not.Null);
            Assert.That(animator.avatar.isValid, Is.True);
            Assert.That(animator.avatar.isHuman, Is.True);
            Assert.That(((ModelImporter)AssetImporter.GetAtPath(Path(actor))).animationType,
                Is.EqualTo(ModelImporterAnimationType.Human));
        }

        [TestCase("Soldier", 0)]
        [TestCase("Soldier", 1)]
        [TestCase("Soldier", 2)]
        [TestCase("Captain", 0)]
        [TestCase("Captain", 1)]
        [TestCase("Captain", 2)]
        public void ControlledImportsExcludeEmbeddedMaterialsAndRootMotion(string actor, int lod)
        {
            Model(actor, lod);
            var importer = (ModelImporter)AssetImporter.GetAtPath(Path(actor, lod));
            Assert.That(importer.materialImportMode, Is.EqualTo(ModelImporterMaterialImportMode.None));
            Assert.That(importer.isReadable, Is.False);
            Assert.That(importer.importCameras, Is.False);
            Assert.That(importer.importLights, Is.False);
            Assert.That(importer.maxBonesPerVertex, Is.LessThanOrEqualTo(4));
            Assert.That(importer.importAnimation, Is.EqualTo(lod == 0));
            foreach (var clip in importer.clipAnimations)
            {
                Assert.That(clip.lockRootPositionXZ, Is.True, clip.name);
                Assert.That(clip.lockRootHeightY, Is.True, clip.name);
                Assert.That(clip.lockRootRotation, Is.True, clip.name);
                Assert.That(clip.events, Is.Empty, clip.name);
            }
        }

        [TestCase("Soldier")]
        [TestCase("Captain")]
        public void RepresentativeClipsAreHumanAndBoundedToPresentation(string actor)
        {
            foreach (string action in Actions)
            {
                var clip = Clip(actor, action);
                Assert.That(clip.isHumanMotion, Is.True, action);
                Assert.That(clip.length, Is.GreaterThan(0).And.LessThanOrEqualTo(1.01f), action);
                Assert.That(AnimationUtility.GetAnimationEvents(clip), Is.Empty, action);
                Assert.That(clip.isLooping, Is.EqualTo(action == "Idle" || action == "Guard"), action);
            }
            Assert.That(Clip(actor, "DodgeLeft").length, Is.EqualTo(.36f).Within(.011f));
            Assert.That(Clip(actor, "CutDown").length, Is.EqualTo(.5f).Within(.011f));
        }

        [Test]
        public void PairUsesTheSameNamedBoneHierarchyAndBindPose()
        {
            var reference = Model("Soldier").GetComponentsInChildren<SkinnedMeshRenderer>().First();
            var skeleton = reference.bones.ToDictionary(b => b.name);
            var poses = reference.bones.Select((bone, i) => new { bone.name, pose = reference.sharedMesh.bindposes[i] })
                .ToDictionary(x => x.name, x => x.pose);
            foreach (string actor in new[] { "Soldier", "Captain" })
            for (int lod = 0; lod < 3; lod++)
            {
                var model = Model(actor, lod);
                Assert.That(model.transform.localScale, Is.EqualTo(Vector3.one));
                foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                    AssertSharedBindPose(renderer, skeleton, poses);
            }
        }

        private static void AssertSharedBindPose(SkinnedMeshRenderer renderer,
            Dictionary<string, Transform> skeleton, Dictionary<string, Matrix4x4> poses)
        {
            var bindposes = renderer.sharedMesh.bindposes;
            Assert.That(bindposes.Length, Is.EqualTo(renderer.bones.Length));
            for (int i = 0; i < renderer.bones.Length; i++)
            {
                var bone = renderer.bones[i];
                Assert.That(skeleton.ContainsKey(bone.name), Is.True, bone.name);
                var reference = skeleton[bone.name];
                Assert.That(Vector3.Distance(bone.localPosition, reference.localPosition), Is.LessThan(.0001f), bone.name);
                Assert.That(Quaternion.Angle(bone.localRotation, reference.localRotation), Is.LessThan(.01f), bone.name);
                Assert.That(Vector3.Distance(bone.localScale, reference.localScale), Is.LessThan(.0001f), bone.name);
                string parentA = skeleton.ContainsKey(reference.parent.name) ? reference.parent.name : "RigRoot";
                string parentB = skeleton.ContainsKey(bone.parent.name) ? bone.parent.name : "RigRoot";
                Assert.That(parentA, Is.EqualTo(parentB), bone.name);
                for (int cell = 0; cell < 16; cell++)
                    Assert.That(bindposes[i][cell], Is.EqualTo(poses[bone.name][cell]).Within(.0001f), bone.name);
            }
        }

        [TestCase("Soldier")]
        [TestCase("Captain")]
        public void LowerLodsReduceGeometryAndKeepEquipmentMarkers(string actor)
        {
            long prior = long.MaxValue;
            for (int lod = 0; lod < 3; lod++)
            {
                var model = Model(actor, lod);
                long triangles = model.GetComponentsInChildren<SkinnedMeshRenderer>()
                    .Sum(r => Enumerable.Range(0, r.sharedMesh.subMeshCount)
                        .Sum(i => (long)r.sharedMesh.GetIndexCount(i) / 3));
                Assert.That(triangles, Is.GreaterThan(0).And.LessThan(prior), actor + " LOD" + lod);
                Assert.That(model.GetComponentsInChildren<Transform>().Any(t => t.name == "WeaponTip"), Is.True);
                prior = triangles;
            }
        }

        [TestCase("Soldier")]
        [TestCase("Captain")]
        public void AssembledPairHasThreeLodsSharingTheAnimatedSkeleton(string actor)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + actor + ".prefab");
            Assert.That(prefab, Is.Not.Null, actor + " assembled prefab");
            var group = prefab.GetComponent<LODGroup>();
            Assert.That(group, Is.Not.Null);
            var lods = group.GetLODs();
            Assert.That(lods.Length, Is.EqualTo(3));
            var animator = prefab.GetComponentInChildren<Animator>();
            Assert.That(prefab.GetComponentsInChildren<Animator>().Length, Is.EqualTo(1));
            Assert.That(animator.applyRootMotion, Is.False);
            foreach (var lod in lods)
            {
                Assert.That(lod.renderers, Is.Not.Empty);
                foreach (var renderer in lod.renderers.Cast<SkinnedMeshRenderer>())
                {
                    Assert.That(renderer.bones.All(b => b != null && b.IsChildOf(animator.transform)), Is.True);
                    Assert.That(renderer.sharedMaterials.All(m => m != null &&
                        m.shader.name == "Universal Render Pipeline/Lit"), Is.True);
                }
            }
        }

        [Test]
        public void SeparateReviewSceneContainsThePairWithinPortraitFraming()
        {
            const string path = "Assets/Game/Scenes/PrototypePairReview.unity";
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null);
            var scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var roots = scene.GetRootGameObjects();
                var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
                camera.aspect = 9f / 16f;
                var characters = roots.Where(r => r.GetComponent<LODGroup>() != null).ToArray();
                Assert.That(characters.Length, Is.EqualTo(2));
                foreach (var character in characters)
                foreach (var renderer in character.GetComponent<LODGroup>().GetLODs()[0].renderers)
                {
                    var bounds = renderer.bounds;
                    foreach (float x in new[] { bounds.min.x, bounds.max.x })
                    foreach (float y in new[] { bounds.min.y, bounds.max.y })
                    foreach (float z in new[] { bounds.min.z, bounds.max.z })
                    {
                        var point = camera.WorldToViewportPoint(new Vector3(x, y, z));
                        Assert.That(point.z, Is.GreaterThan(0));
                        Assert.That(point.x, Is.InRange(0, 1), renderer.name);
                        Assert.That(point.y, Is.InRange(0, 1), renderer.name);
                    }
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [TestCase("CutUp")]
        [TestCase("CutDown")]
        [TestCase("CutLeft")]
        [TestCase("CutRight")]
        public void SoldierCutsRetargetOntoBothAvatarsWithoutMovingTheRoot(string action)
        {
            var clip = Clip("Soldier", action);
            foreach (string actor in new[] { "Soldier", "Captain" })
            {
                var owner = Object.Instantiate(Model(actor));
                var graph = PlayableGraph.Create("Prototype retarget check");
                try
                {
                    var animator = owner.GetComponent<Animator>();
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.Rebind();
                    var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    Assert.That(hand, Is.Not.Null);
                    var initialHand = hand.position;
                    var initialRoot = owner.transform.position;
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var playable = AnimationClipPlayable.Create(graph, clip);
                    AnimationPlayableOutput.Create(graph, "Character", animator).SetSourcePlayable(playable);
                    graph.Play();
                    graph.Evaluate(.1f);
                    Assert.That(Vector3.Distance(initialHand, hand.position), Is.GreaterThan(.015f), actor + " " + action);
                    Assert.That(owner.transform.position, Is.EqualTo(initialRoot));
                    foreach (var renderer in owner.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var baked = new Mesh();
                        try
                        {
                            renderer.BakeMesh(baked);
                            Assert.That(baked.vertexCount, Is.GreaterThan(0));
                            Assert.That(baked.vertices.All(v => !float.IsNaN(v.x) && !float.IsInfinity(v.x)
                                && !float.IsNaN(v.y) && !float.IsInfinity(v.y)
                                && !float.IsNaN(v.z) && !float.IsInfinity(v.z)), Is.True, actor + " deformation");
                        }
                        finally { Object.DestroyImmediate(baked); }
                    }
                }
                finally
                {
                    if (graph.IsValid()) graph.Destroy();
                    Object.DestroyImmediate(owner);
                }
            }
        }
    }
}
