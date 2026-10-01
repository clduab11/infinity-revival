using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class WeaponPilotImportTests
    {
        private const string Folder = "Assets/Game/Content/Characters/WeaponFamilyPilot/";
        private const string AcceptedFolder = "Assets/Game/Content/Characters/PrototypePair/";
        private static readonly string[] Keys = { "Idle", "Guard", "Parry", "DodgeLeft", "DodgeRight",
            "CutUp", "CutDown", "CutLeft", "CutRight", "Hit", "Death", "EnemyTell", "EnemyAttack" };

        private static GameObject Model(string actor, int lod = 0)
        {
            string path = Folder + actor + "_LOD" + lod + ".fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(model, Is.Not.Null, "Missing original family pilot: " + path);
            return model;
        }

        private static AnimationClip Clip(string actor, string key) =>
            AssetDatabase.LoadAllAssetsAtPath(Folder + actor + "_LOD0.fbx")
                .OfType<AnimationClip>().Single(c => c.name == key);

        [TestCase("AxePilot", 0)]
        [TestCase("AxePilot", 1)]
        [TestCase("AxePilot", 2)]
        [TestCase("MacePilot", 0)]
        [TestCase("MacePilot", 1)]
        [TestCase("MacePilot", 2)]
        public void FamilyLodsHaveControlledHumanoidImports(string actor, int lod)
        {
            var model = Model(actor, lod);
            var animator = model.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
            var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + actor + "_LOD" + lod + ".fbx");
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human));
            Assert.That(importer.materialImportMode, Is.EqualTo(ModelImporterMaterialImportMode.None));
            Assert.That(importer.isReadable || importer.importCameras || importer.importLights, Is.False);
            Assert.That(importer.optimizeGameObjects, Is.False);
            Assert.That(importer.maxBonesPerVertex, Is.LessThanOrEqualTo(4));
            Assert.That(importer.importAnimation, Is.EqualTo(lod == 0));
            foreach (var binding in importer.clipAnimations)
            {
                Assert.That(binding.lockRootPositionXZ && binding.lockRootHeightY && binding.lockRootRotation, Is.True);
                Assert.That(binding.events, Is.Empty);
            }
            var transforms = model.GetComponentsInChildren<Transform>();
            Assert.That(transforms.Count(t => t.name == "WeaponTip"), Is.EqualTo(1));
            Assert.That(transforms.Count(t => t.name == "WeaponSocket"), Is.EqualTo(1));
            Assert.That(transforms.Count(t => t.name == "ShieldSocket"), Is.EqualTo(1));
        }

        [TestCase("AxePilot", .6)]
        [TestCase("MacePilot", .7)]
        public void EveryFamilyHasThirteenHumanClipsWithItsOwnAttackDuration(string actor, double duration)
        {
            Model(actor);
            var clips = AssetDatabase.LoadAllAssetsAtPath(Folder + actor + "_LOD0.fbx")
                .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            Assert.That(clips.Select(c => c.name), Is.EquivalentTo(Keys));
            foreach (var clip in clips)
            {
                Assert.That(clip.isHumanMotion, Is.True, clip.name);
                Assert.That(AnimationUtility.GetAnimationEvents(clip), Is.Empty, clip.name);
                Assert.That(clip.isLooping, Is.EqualTo(clip.name == "Idle" || clip.name == "Guard"), clip.name);
                if (clip.name.StartsWith("Cut", StringComparison.Ordinal) || clip.name == "EnemyAttack")
                    Assert.That(clip.length, Is.EqualTo(duration).Within(.011), clip.name);
            }
        }

        [TestCase("AxePilot")]
        [TestCase("MacePilot")]
        public void FamilyPrefabsShareTheAcceptedBindPoseAndOneAnimatedSkeleton(string actor)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + actor + ".prefab");
            Assert.That(prefab, Is.Not.Null);
            var animator = prefab.GetComponentsInChildren<Animator>().Single();
            Assert.That(animator.transform.name, Is.EqualTo("LOD0"));
            Assert.That(animator.applyRootMotion, Is.False);
            var accepted = AssetDatabase.LoadAssetAtPath<GameObject>(AcceptedFolder + "Soldier_LOD0.fbx");
            var reference = accepted.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name.EndsWith("_Body"));
            var poses = reference.bones.Select((bone, i) => new { bone.name, Pose = reference.sharedMesh.bindposes[i] })
                .ToDictionary(b => b.name, b => b.Pose);
            var bones = reference.bones.ToDictionary(b => b.name);
            var lods = prefab.GetComponent<LODGroup>().GetLODs();
            Assert.That(lods.Length, Is.EqualTo(3));
            long prior = long.MaxValue;
            foreach (var lod in lods)
            {
                Assert.That(lod.renderers.Length, Is.EqualTo(3));
                long triangles = 0;
                foreach (var renderer in lod.renderers.Cast<SkinnedMeshRenderer>())
                {
                    Assert.That(renderer.bones.All(b => b != null && b.IsChildOf(animator.transform)), Is.True);
                    Assert.That(renderer.sharedMaterials.All(m => m != null && m.shader.name == "Universal Render Pipeline/Lit"), Is.True);
                    triangles += Enumerable.Range(0, renderer.sharedMesh.subMeshCount).Sum(i => (long)renderer.sharedMesh.GetIndexCount(i) / 3);
                    for (int i = 0; i < renderer.bones.Length; i++)
                    {
                        var bone = renderer.bones[i];
                        Assert.That(bones.ContainsKey(bone.name), Is.True, bone.name);
                        Assert.That(Vector3.Distance(bone.localPosition, bones[bone.name].localPosition), Is.LessThan(.0001f));
                        Assert.That(Quaternion.Angle(bone.localRotation, bones[bone.name].localRotation), Is.LessThan(.01f));
                        for (int cell = 0; cell < 16; cell++)
                            Assert.That(renderer.sharedMesh.bindposes[i][cell], Is.EqualTo(poses[bone.name][cell]).Within(.0001f));
                    }
                }
                Assert.That(triangles, Is.GreaterThan(0).And.LessThan(prior));
                prior = triangles;
            }
            Assert.That(prefab.GetComponentsInChildren<Transform>().Count(t => t.name == "WeaponTip"), Is.EqualTo(1),
                "The assembled weapon instance needs one unambiguous contact marker.");
            var tip = animator.GetComponentsInChildren<Transform>().Single(t => t.name == "WeaponTip");
            Assert.That(tip.parent.name, Is.EqualTo("WeaponSocket"));
            Assert.That(tip.IsChildOf(animator.GetBoneTransform(HumanBodyBones.RightHand)), Is.True);
            var shieldSocket = animator.GetComponentsInChildren<Transform>().Single(t => t.name == "ShieldSocket");
            Assert.That(shieldSocket.IsChildOf(animator.GetBoneTransform(HumanBodyBones.LeftHand)), Is.True);
        }

        [TestCase("AxePilot", .15, .6, "CutUp")]
        [TestCase("AxePilot", .15, .6, "CutDown")]
        [TestCase("AxePilot", .15, .6, "CutLeft")]
        [TestCase("AxePilot", .15, .6, "CutRight")]
        [TestCase("MacePilot", .18, .7, "CutUp")]
        [TestCase("MacePilot", .18, .7, "CutDown")]
        [TestCase("MacePilot", .18, .7, "CutLeft")]
        [TestCase("MacePilot", .18, .7, "CutRight")]
        public void FamilyHeadsTravelWithTheCutAndContinuePastContact(string actor, double contact, double duration, string cut)
        {
            Model(actor);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + actor + ".prefab");
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab);
            var graph = PlayableGraph.Create("Family head trajectory acceptance");
            try
            {
                var animator = instance.GetComponentInChildren<Animator>();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                var tip = animator.GetComponentsInChildren<Transform>().Single(t => t.name == "WeaponTip");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable = AnimationClipPlayable.Create(graph, Clip(actor, cut));
                playable.SetSpeed(0);
                AnimationPlayableOutput.Create(graph, "Head path", animator).SetSourcePlayable(playable);
                graph.Play();
                var samples = new List<Vector3>();
                foreach (double time in new[] { 0, contact * .6, contact, contact + .06, contact + .12, duration })
                {
                    playable.SetTime(time); graph.Evaluate(0);
                    samples.Add(instance.transform.InverseTransformPoint(tip.position));
                }
                SaveTrace(actor, cut, contact, samples);
                var travel = samples[3] - samples[0];
                double directed = cut == "CutUp" ? travel.y : cut == "CutDown" ? -travel.y : cut == "CutLeft" ? -travel.x : travel.x;
                Assert.That(directed, Is.GreaterThan(.1), "The striking head must follow the captured swipe direction.");
                Assert.That(Vector3.Distance(samples[2], samples[4]), Is.GreaterThan(.025f), "Contact must continue into follow-through.");
            }
            finally { graph.Destroy(); Object.DestroyImmediate(instance); }
        }

        [TestCase("AxePilot", "Soldier")]
        [TestCase("AxePilot", "Captain")]
        [TestCase("MacePilot", "Soldier")]
        [TestCase("MacePilot", "Captain")]
        public void FamilyCutsRetargetOntoBothAcceptedAvatarsWithoutRootDrift(string actor, string target)
        {
            Model(actor);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(AcceptedFolder + target + "_LOD0.fbx");
            foreach (string cut in Keys.Where(k => k.StartsWith("Cut", StringComparison.Ordinal)))
            {
                var instance = Object.Instantiate(source);
                var graph = PlayableGraph.Create("Family Humanoid retarget acceptance");
                try
                {
                    var animator = instance.GetComponent<Animator>();
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.Rebind();
                    var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    var before = hand.position;
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var playable = AnimationClipPlayable.Create(graph, Clip(actor, cut));
                    playable.SetSpeed(0); playable.SetTime(actor == "AxePilot" ? .15 : .18);
                    AnimationPlayableOutput.Create(graph, "Retarget", animator).SetSourcePlayable(playable);
                    graph.Play(); graph.Evaluate(0);
                    Assert.That(Vector3.Distance(before, hand.position), Is.GreaterThan(.015f));
                    Assert.That(instance.transform.position, Is.EqualTo(Vector3.zero));
                    foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var mesh = new Mesh();
                        try
                        {
                            renderer.BakeMesh(mesh);
                            Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                            Assert.That(mesh.vertices.All(v => Finite(v.x) && Finite(v.y) && Finite(v.z)), Is.True);
                        }
                        finally { Object.DestroyImmediate(mesh); }
                    }
                }
                finally { graph.Destroy(); Object.DestroyImmediate(instance); }
            }
        }

        [TestCase("AxePilot")]
        [TestCase("MacePilot")]
        public void OriginalFamilySilhouettesHaveAStrikingHeadAndStableLodGrip(string actor)
        {
            var shapes = new List<Bounds>();
            for (int lod = 0; lod < 3; lod++)
            {
                var source = Model(actor, lod);
                var renderer = source.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name.EndsWith("_Weapon"));
                // The shared FBX palette contains all bones; vertex influences establish rigidity.
                var weights = renderer.sharedMesh.GetAllBoneWeights();
                Assert.That(weights.Length, Is.EqualTo(renderer.sharedMesh.vertexCount));
                foreach (var influence in weights)
                {
                    Assert.That(influence.weight, Is.EqualTo(1f).Within(.000001f));
                    Assert.That(renderer.bones[influence.boneIndex].name, Is.EqualTo("RightHand"));
                }
                // Renderer bounds include the full animation envelope on LOD0 only.
                // Rest mesh bounds compare the actual exported silhouettes across all LODs.
                shapes.Add(renderer.sharedMesh.bounds);
            }
            foreach (var bounds in shapes.Skip(1))
            {
                Assert.That(Vector3.Distance(bounds.center, shapes[0].center), Is.LessThan(.03f));
                Assert.That(Vector3.Distance(bounds.size, shapes[0].size), Is.LessThan(.05f));
            }
            if (actor == "AxePilot") Assert.That(shapes[0].size.x, Is.GreaterThan(.22f), "Axe needs a broad asymmetric head.");
            else
            {
                Assert.That(shapes[0].size.x, Is.GreaterThan(.13f), "Mace needs a readable striking head.");
                Assert.That(shapes[0].size.y, Is.LessThan(.68f), "Mace silhouette must retain shorter apparent reach.");
            }
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void SaveTrace(string actor, string cut, double contact, List<Vector3> samples)
        {
            string folder = Environment.GetEnvironmentVariable("PRAXEN_TEST_OUTPUT");
            if (string.IsNullOrEmpty(folder)) return;
            File.WriteAllText(Path.Combine(folder, "weapon-path-" + actor + "-" + cut + ".json"),
                JsonUtility.ToJson(new PathEvidence { Actor = actor, Cut = cut, ContactSeconds = contact, Positions = samples.ToArray() }, true));
        }

        [Serializable]
        private sealed class PathEvidence
        { public string Actor; public string Cut; public double ContactSeconds; public Vector3[] Positions; }
    }
}
