using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Praxen.Game.Application.Combat;
using Praxen.Game.Content.Definitions;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using Praxen.Game.Presentation.Combat;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Praxen.Game.Tests.PlayMode
{
    public sealed class Task13ContentAdmissionTests
    {
        private GrayboxEncounterRoot root;
        private InputSettings.UpdateMode previousMode;
        private long now;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousMode = InputSystem.settings.updateMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            yield return SceneManager.LoadSceneAsync("CombatPrototype", LoadSceneMode.Single);
            root = Object.FindAnyObjectByType<GrayboxEncounterRoot>();
            root.SetLifecycle(false, true);
            now = 0;
            root.RestartEncounter(() => now);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) root.Timing.EndEncounter();
            InputSystem.settings.updateMode = previousMode;
            yield return null;
        }

        [Test]
        public void EncounterRootExposesAnAuthoredCatalogAndCapturedContent()
        {
            Assert.That(typeof(GrayboxEncounterRoot).GetField("contentCatalog",
                BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(root.ContentCatalog, Is.Not.Null);
            Assert.That(root.ActiveContent, Is.Not.Null);
            Assert.That(root.ActiveContent.Weapon.Id, Is.EqualTo("weapon.pilot.sword"));
            Assert.That(root.ActiveContent.EnemyDeck.ContentRevision, Is.EqualTo(root.EnemyAI.ContentRevision));
        }

        [Test]
        public void InvalidCatalogLeavesTheActiveEncounterAndSelectionIntact()
        {
            var encounter = root.Encounter;
            var director = root.EnemyAI;
            var snapshot = root.ActiveContent;
            var attack = root.ContentCatalog.GetWeapon(root.SelectedWeaponId).Attacks[0];
            long windup = attack.WindupUs;
            try
            {
                attack.WindupUs = 0;
                Assert.Catch<InvalidOperationException>(() => root.RestartEncounter(() => now,
                    444, EnemyArchetype.Hammer, 2, "weapon.pilot.axe"));
                Assert.That(root.Encounter, Is.SameAs(encounter));
                Assert.That(root.EnemyAI, Is.SameAs(director));
                Assert.That(root.ActiveContent, Is.SameAs(snapshot));
                Assert.That(root.SelectedWeaponId, Is.EqualTo("weapon.pilot.sword"));
                Assert.That(root.Timing.Session, Is.SameAs(encounter.Session));
            }
            finally { attack.WindupUs = windup; }
        }

        [Test]
        public void InvalidLegacyCameraCannotDestroyAnActiveEncounter()
        {
            var catalogField = typeof(GrayboxEncounterRoot).GetField("contentCatalog", BindingFlags.Instance | BindingFlags.NonPublic);
            var profile = (CombatPresentationProfile)typeof(GrayboxEncounterRoot).GetField("presentationProfile",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root);
            var catalog = root.ContentCatalog;
            float degrees = profile.CameraOrbitDegrees;
            var session = root.Timing.Session;
            var snapshot = root.ActiveContent;
            try
            {
                catalogField.SetValue(root, null);
                profile.CameraOrbitDegrees = float.NaN;
                Assert.Catch<InvalidOperationException>(() => root.RestartEncounter(seed: 444));
                Assert.That(root.Timing.Session, Is.SameAs(session));
                Assert.That(root.ActiveContent, Is.SameAs(snapshot));
                Assert.That(root.Presentation.PlayerAnimation.GraphIsValid, Is.True);
            }
            finally { profile.CameraOrbitDegrees = degrees; catalogField.SetValue(root, catalog); }
        }

        [Test]
        public void InvalidLegacyAvatarCannotDestroyAnActiveEncounter()
        {
            var catalogField = typeof(GrayboxEncounterRoot).GetField("contentCatalog", BindingFlags.Instance | BindingFlags.NonPublic);
            var profile = (CombatPresentationProfile)typeof(GrayboxEncounterRoot).GetField("presentationProfile",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root);
            var catalog = root.ContentCatalog;
            var original = profile.Soldier;
            var invalid = new GameObject("Invalid humanoid admission");
            var session = root.Timing.Session;
            var animation = root.Presentation.PlayerAnimation;
            try
            {
                catalogField.SetValue(root, null);
                profile.Soldier = invalid;
                Assert.Catch<InvalidOperationException>(() => root.RestartEncounter(seed: 444));
                Assert.That(root.Timing.Session, Is.SameAs(session));
                Assert.That(root.Presentation.PlayerAnimation, Is.SameAs(animation));
                Assert.That(animation.GraphIsValid, Is.True);
            }
            finally { profile.Soldier = original; catalogField.SetValue(root, catalog); Object.Destroy(invalid); }
        }

        [Test]
        public void UncommittedPreparedConfigurationPreservesTheBoundPresentation()
        {
            var profile = (CombatPresentationProfile)typeof(GrayboxEncounterRoot).GetField("presentationProfile",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root);
            var animation = root.Presentation.PlayerAnimation;
            using (var prepared = root.Presentation.PrepareConfiguration(profile))
                Assert.Catch<InvalidOperationException>(() => prepared.Commit());
            Assert.That(root.Presentation.PlayerAnimation, Is.SameAs(animation));
            Assert.That(animation.GraphIsValid, Is.True);
            Assert.That(root.View.PlayerAnchor.GetComponentsInChildren<Animator>().Length, Is.EqualTo(1));
        }

        [Test]
        public void UnknownWeaponCannotReplaceTheActiveEncounter()
        {
            var session = root.Timing.Session;
            Assert.Throws<ArgumentException>(() => root.RestartEncounter(selectedWeaponId: "weapon.missing"));
            Assert.That(root.Timing.Session, Is.SameAs(session));
            Assert.That(root.SelectedWeaponId, Is.EqualTo("weapon.pilot.sword"));
        }

        [Test]
        public void InvalidEnemyMotionCannotReplaceTheActiveEncounter()
        {
            var field = typeof(GrayboxEncounterRoot).GetField("enemyMotionProfile",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var motion = (MotionProfileDefinition)field.GetValue(root);
            var path = motion.WeaponTipPath;
            var session = root.Timing.Session;
            try
            {
                motion.WeaponTipPath = "LOD1/MissingTip";
                Assert.Catch<InvalidOperationException>(() => root.RestartEncounter(selectedWeaponId: "weapon.pilot.axe"));
                Assert.That(root.Timing.Session, Is.SameAs(session));
            }
            finally { motion.WeaponTipPath = path; }
        }

        [TestCase("sword", .1, 100000)]
        [TestCase("axe", .15, 150000)]
        [TestCase("mace", .18, 180000)]
        public void FamilyContactIsSampledAtItsLogicalImpact(string family, double contact, long windup)
        {
            root.RestartEncounter(() => now, selectedWeaponId: "weapon.pilot." + family);
            StepTo(1160000);
            Attack(1, 1200000, SwipeDirection.Right);
            StepTo(1200000 + windup);
            Assert.That(root.Presentation.PlayerAnimation.MotionKey, Is.EqualTo("CutRight"));
            Assert.That(root.Presentation.PlayerAnimation.SampleTimeSeconds, Is.EqualTo(contact).Within(.00001));
            Assert.That(root.ActiveContent.Offense.WindupUs, Is.EqualTo(windup));
            Assert.That(root.Duel.Offense.EnemyHealth, Is.EqualTo(80));
            Assert.That(root.View.PlayerAnchor.GetComponentsInChildren<Animator>().Length, Is.EqualTo(1));
        }

        [Test]
        public void ActiveAuthoringEditsCannotChangeContactEvenAfterGraphRecreation()
        {
            root.RestartEncounter(() => now, selectedWeaponId: "weapon.pilot.axe");
            var motion = root.ContentCatalog.GetWeapon(root.SelectedWeaponId).MotionProfile;
            var bindings = (MotionBinding[])motion.Bindings.Clone();
            try
            {
                for (int i = 0; i < motion.Bindings.Length; i++)
                {
                    var binding = motion.Bindings[i];
                    if (binding.Key == "CutRight") { binding.ContactSeconds = .12; motion.Bindings[i] = binding; }
                }
                StepTo(1160000); Attack(1, 1200000, SwipeDirection.Right); StepTo(1350000);
                Assert.That(root.Presentation.PlayerAnimation.SampleTimeSeconds, Is.EqualTo(.15).Within(.00001));
                root.Presentation.enabled = false;
                root.Presentation.enabled = true;
                root.Presentation.RenderCurrent();
                Assert.That(root.Presentation.PlayerAnimation.SampleTimeSeconds, Is.EqualTo(.15).Within(.00001));
            }
            finally { motion.Bindings = bindings; }
        }

        [Test]
        public void RevisedLogicalTimingIsCapturedOnlyOnTheNextRestart()
        {
            var attacks = root.ContentCatalog.GetWeapon(root.SelectedWeaponId).Attacks;
            var snapshot = root.ActiveContent;
            var original = new long[attacks.Length];
            try
            {
                for (int i = 0; i < attacks.Length; i++)
                { original[i] = attacks[i].WindupUs; attacks[i].WindupUs += 20000; }
                Assert.That(root.Duel.Offense.Tuning.WindupUs, Is.EqualTo(100000));
                Assert.That(snapshot.Offense.WindupUs, Is.EqualTo(100000));
                root.RestartEncounter(() => now);
                Assert.That(root.ActiveContent.Offense.WindupUs, Is.EqualTo(120000));
                Assert.That(root.ActiveContent, Is.Not.SameAs(snapshot));
                Assert.That(snapshot.Offense.WindupUs, Is.EqualTo(100000));
            }
            finally { for (int i = 0; i < attacks.Length; i++) attacks[i].WindupUs = original[i]; }
        }

        [TestCase("sword")]
        [TestCase("axe")]
        [TestCase("mace")]
        public void FamilyReplayIsIdenticalAcrossCadencesAndPresentationState(string family)
        {
            List<string> baseline = null;
            var evidence = new List<FamilyReplay>();
            foreach (int fps in new[] { 30, 60, 120, 0 })
            foreach (bool enabled in new[] { true, false })
            {
                now = 0;
                root.RestartEncounter(() => now, 20260929, EnemyArchetype.Sword, 0, "weapon.pilot." + family);
                root.Presentation.enabled = enabled;
                var trace = new List<string>();
                root.Duel.PlayerAttackStarted += attack => trace.Add($"attack:{attack.StartedUs}:{attack.ImpactUs}:{attack.RecoveryEndUs}");
                root.Duel.PlayerStrikeResolved += hit => trace.Add($"hit:{hit.TimeUs}:{hit.Damage}:{hit.EnemyHealth}");
                int frame = 0;
                bool delivered = false;
                var jitter = new long[] { 11000, 49000, 7000, 81000, 23000 };
                while (now < 2200000)
                {
                    long next = Math.Min(2200000, fps == 0 ? now + jitter[frame % jitter.Length] :
                        (long)Math.Round((frame + 1) * 1000000d / fps));
                    frame++;
                    var input = !delivered && next >= 1200000
                        ? new[] { Gesture(1, 1200000, SwipeDirection.Right) } : Array.Empty<CombatInput>();
                    if (input.Length > 0) delivered = true;
                    now = next;
                    root.Timing.Session.AdvanceInputFrame(now, input);
                    root.Presentation.RenderCurrent();
                }
                trace.Add($"final:{root.Duel.Offense.EnemyHealth}:{root.Encounter.Player.Health}:{root.Duel.CurrentPhase.Kind}");
                evidence.Add(new FamilyReplay { Fps = fps, PresentationEnabled = enabled, Trace = trace.ToArray() });
                Assert.That(trace.Count, Is.EqualTo(3));
                Assert.That(root.Duel.Offense.EnemyHealth, Is.EqualTo(80));
                if (baseline == null) baseline = trace;
                else Assert.That(trace, Is.EqualTo(baseline), family + " " + fps + " " + enabled);
            }
            var output = Environment.GetEnvironmentVariable("PRAXEN_TEST_OUTPUT");
            if (!string.IsNullOrEmpty(output)) File.WriteAllText(Path.Combine(output, "family-replay-" + family + ".json"),
                JsonUtility.ToJson(new FamilyReplayEvidence { Family = family, Replays = evidence.ToArray() }, true));
        }

        [UnityTest]
        public IEnumerator FamilyPilotsRenderThroughNativeUrp()
        {
            foreach (string family in new[] { "sword", "axe", "mace" })
            {
                now = 0;
                root.RestartEncounter(() => now, selectedWeaponId: "weapon.pilot." + family);
                yield return GrayboxCapture.WarmFrame(root.EncounterCamera);
                yield return PortraitHudCapture.Save(root.EncounterCamera, 941, 1672, "task13-" + family + "-idle.png");
                StepTo(1160000);
                Attack(1, 1200000, SwipeDirection.Right);
                StepTo(1200000 + root.ActiveContent.Offense.WindupUs);
                yield return PortraitHudCapture.Save(root.EncounterCamera, 941, 1672, "task13-" + family + "-contact.png");
            }
        }

        [Serializable]
        private sealed class FamilyReplayEvidence { public string Family; public FamilyReplay[] Replays; }
        [Serializable]
        private sealed class FamilyReplay { public int Fps; public bool PresentationEnabled; public string[] Trace; }

        private CombatInput Gesture(long sequence, long time, SwipeDirection direction) =>
            new CombatInput(new GestureCommand(sequence, time, 1, direction, GestureIntent.Attack,
                root.Duel.CurrentPhase, new NormalizedPoint(.5, .5), new NormalizedPoint(.7, .5)));
        private void Attack(long sequence, long time, SwipeDirection direction)
        {
            now = time;
            root.Timing.Session.AdvanceInputFrame(now, new[] { Gesture(sequence, time, direction) });
            root.Presentation.RenderCurrent();
        }
        private void StepTo(long target)
        {
            while (now < target)
            {
                now = Math.Min(target, now + 50000);
                root.Timing.Session.AdvanceInputFrame(now, Array.Empty<CombatInput>());
                root.Presentation.RenderCurrent();
            }
        }
    }
}
