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
    public sealed class PrototypeSwordMotionTests
    {
        [TestCase("Soldier", 0)]
        [TestCase("Soldier", 1)]
        [TestCase("Soldier", 2)]
        [TestCase("Captain", 0)]
        [TestCase("Captain", 1)]
        [TestCase("Captain", 2)]
        public void ImportedRestPoseUsesAnatomicalHandSides(string actor, int lod)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Content/Characters/PrototypePair/" + actor + "_LOD" + lod + ".fbx");
            var instance = Object.Instantiate(source);
            try
            {
                var animator = instance.GetComponentInChildren<Animator>();
                animator.Rebind();
                var right = instance.transform.InverseTransformPoint(
                    animator.GetBoneTransform(HumanBodyBones.RightHand).position);
                var left = instance.transform.InverseTransformPoint(
                    animator.GetBoneTransform(HumanBodyBones.LeftHand).position);
                Assert.That(right.x, Is.GreaterThan(.4f), "Anatomical right must match the actor's right axis.");
                Assert.That(left.x, Is.LessThan(-.4f), "Anatomical left must match the actor's left axis.");
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [TestCase("Soldier", "CutUp")]
        [TestCase("Soldier", "CutDown")]
        [TestCase("Soldier", "CutLeft")]
        [TestCase("Soldier", "CutRight")]
        [TestCase("Captain", "CutUp")]
        [TestCase("Captain", "CutDown")]
        [TestCase("Captain", "CutLeft")]
        [TestCase("Captain", "CutRight")]
        public void SwordCutsHaveDirectionalTravelAndMovingFollowThrough(string actor, string cut)
        {
            const string folder = "Assets/Game/Content/Characters/PrototypePair/";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + actor + ".prefab");
            var clip = AssetDatabase.LoadAllAssetsAtPath(folder + actor + "_LOD0.fbx")
                .OfType<AnimationClip>().Single(c => c.name == cut);
            var instance = Object.Instantiate(prefab);
            var graph = PlayableGraph.Create("Sword trajectory acceptance");
            var samples = new List<Vector3>();
            try
            {
                var animator = instance.GetComponentInChildren<Animator>();
                var tip = animator.GetComponentsInChildren<Transform>().Single(t => t.name == "WeaponTip");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetSpeed(0);
                var output = AnimationPlayableOutput.Create(graph, "Trajectory", animator);
                output.SetSourcePlayable(playable);
                graph.Play();
                foreach (double time in new double[] { 0, .06, .1, .14, .18, .5 })
                {
                    playable.SetTime(time);
                    graph.Evaluate(0);
                    samples.Add(instance.transform.InverseTransformPoint(tip.position));
                }
                SaveTrace(actor, cut, samples);
                Assert.That(Vector3.Distance(samples[2], samples[4]), Is.GreaterThan(.025f),
                    "The blade must continue past contact instead of holding then rewinding.");
                var travel = samples[3] - samples[0];
                float directedTravel = cut == "CutUp" ? travel.y : cut == "CutDown" ? -travel.y :
                    cut == "CutLeft" ? -travel.x : travel.x;
                Assert.That(directedTravel, Is.GreaterThan(.1f), "The visible blade must follow the swipe direction.");
            }
            finally { graph.Destroy(); Object.DestroyImmediate(instance); }
        }

        private static void SaveTrace(string actor, string cut, List<Vector3> samples)
        {
            string folder = Environment.GetEnvironmentVariable("PRAXEN_TEST_OUTPUT");
            if (string.IsNullOrEmpty(folder)) return;
            File.WriteAllText(Path.Combine(folder, "sword-path-" + actor + "-" + cut + ".json"),
                JsonUtility.ToJson(new PathEvidence { Actor = actor, Cut = cut, Positions = samples.ToArray() }, true));
        }

        [Serializable]
        private sealed class PathEvidence
        { public string Actor; public string Cut; public Vector3[] Positions; }
    }
}
