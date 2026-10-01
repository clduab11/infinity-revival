using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Praxen.Game.Presentation.Combat;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Praxen.Game.Tests.PlayMode
{
    public sealed class Task13PresentationSnapshotTests
    {
        [Test]
        public void MotionBindingDeclaresAnExplicitAuthoredContactTime()
        {
            var field = typeof(MotionClipBinding).GetField("ContactSeconds");
            Assert.That(field, Is.Not.Null,
                "Weapon families need their authored contact time instead of a universal sword contact.");
            Assert.That(field.FieldType, Is.EqualTo(typeof(double)));
        }

        [TestCase(.15, .6)]
        [TestCase(.18, .7)]
        public void LogicalImpactSamplesTheSelectedFamilyContact(double contact, double duration)
        {
            var method = typeof(ActorMotion).GetMethod("SampleSeconds", new[]
                { typeof(long), typeof(double), typeof(double) });
            Assert.That(method, Is.Not.Null, "Presentation must accept an explicit family contact time.");
            var motion = new ActorMotion("CutRight", 100000, 700000, 250000);
            Assert.That((double)method.Invoke(motion, new object[] { 250000L, duration, contact }),
                Is.EqualTo(contact).Within(.000001));
            Assert.That((double)method.Invoke(motion, new object[] { 700000L, duration, contact }),
                Is.EqualTo(duration).Within(.000001));
        }

        [Test]
        public void PresentationSupportsDeliberateUnboundConfigurationRefresh()
        {
            Assert.That(typeof(CombatPresentation).GetMethod("RefreshConfiguration", new[]
                { typeof(CombatPresentationProfile) }), Is.Not.Null,
                "A deliberate encounter restart must be able to select a new weapon family.");
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-.1)]
        public void ContactSamplingRejectsInvalidPresentationTime(double contact)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ActorMotion("CutRight", 0, 500000, 100000).SampleSeconds(100000, .5, contact));
        }
    }

    public sealed class Task13PresentationLifecycleSnapshotTests
    {
        private GrayboxEncounterRoot root;
        private CombatPresentationProfile configuration;
        private AudioClip sound;
        private InputSettings.UpdateMode previousMode;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousMode = InputSystem.settings.updateMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            yield return SceneManager.LoadSceneAsync("CombatPrototype", LoadSceneMode.Single);
            root = Object.FindAnyObjectByType<GrayboxEncounterRoot>();
            root.SetLifecycle(false, true);
            root.RestartEncounter(() => 0);
            configuration = Object.Instantiate((CombatPresentationProfile)Private("profile"));
            configuration.EnableCameraOrbit = true;
            configuration.CameraOrbitDegrees = 3;
            configuration.CameraFocusDistance = 8;
            configuration.ReducedMotion = false;
            for (int i = 0; i < configuration.SoldierMotions.Length; i++)
                if (configuration.SoldierMotions[i].Key == "CutRight")
                    configuration.SoldierMotions[i].ContactSeconds = .15;
            sound = AudioClip.Create("Owned snapshot test cue", 128, 1, 44100, false);
            configuration.Sounds = new[] { new CombatSoundBinding { Kind = CombatCueKind.Hit, Clip = sound } };
            Refresh();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) root.Timing.EndEncounter();
            Object.Destroy(configuration);
            Object.Destroy(sound);
            InputSystem.settings.updateMode = previousMode;
            yield return null;
        }

        [Test]
        public void ActiveAndRecreatedGraphsKeepCapturedClipAndContactRecords()
        {
            var motion = new ActorMotion("CutRight", 100000, 700000, 250000);
            root.Presentation.PlayerAnimation.Render(motion, 250000);
            Assert.That(root.Presentation.PlayerAnimation.SampleTimeSeconds, Is.EqualTo(.15).Within(.000001));
            int index = Array.FindIndex(configuration.SoldierMotions, binding => binding.Key == "CutRight");
            configuration.SoldierMotions[index].ContactSeconds = .3;
            configuration.SoldierMotions[index].Clip = null;
            configuration.SoldierMotions = Array.Empty<MotionClipBinding>();
            root.Presentation.PlayerAnimation.Render(motion, 250000);
            Assert.That(root.Presentation.PlayerAnimation.SampleTimeSeconds, Is.EqualTo(.15).Within(.000001));
            var old = root.Presentation.PlayerAnimation;
            root.Presentation.enabled = false;
            root.Presentation.enabled = true;
            root.Presentation.PlayerAnimation.Render(motion, 250000);
            Assert.That(old.GraphIsValid, Is.False);
            Assert.That(root.Presentation.PlayerAnimation.MotionKey, Is.EqualTo("CutRight"));
            Assert.That(root.Presentation.PlayerAnimation.SampleTimeSeconds, Is.EqualTo(.15).Within(.000001));
        }

        [Test]
        public void CameraAuthoringEditsAreFrozenButExplicitAccessibilityPreferencesApply()
        {
            var motion = new ActorMotion("CutRight", 100000, 700000, 250000);
            Invoke("RenderCamera", motion, 250000L);
            var position = root.EncounterCamera.transform.localPosition;
            var rotation = root.EncounterCamera.transform.localRotation;
            configuration.EnableCameraOrbit = false;
            configuration.CameraOrbitDegrees = 0;
            configuration.CameraFocusDistance = 1;
            configuration.ReducedMotion = true;
            Invoke("RenderCamera", motion, 250000L);
            Assert.That(root.EncounterCamera.transform.localPosition, Is.EqualTo(position));
            Assert.That(root.EncounterCamera.transform.localRotation, Is.EqualTo(rotation));
            root.Presentation.enabled = false;
            root.Presentation.enabled = true;
            Invoke("RenderCamera", motion, 250000L);
            Assert.That(root.EncounterCamera.transform.localPosition, Is.EqualTo(position));
            Assert.That(root.EncounterCamera.transform.localRotation, Is.EqualTo(rotation));
            root.Presentation.ApplyCameraPreferences(true, 3, 8, true);
            Invoke("RenderCamera", motion, 250000L);
            Assert.That(root.EncounterCamera.transform.localPosition, Is.EqualTo((Vector3)Private("cameraPosition")));
            Assert.That(root.EncounterCamera.transform.localRotation, Is.EqualTo((Quaternion)Private("cameraRotation")));
        }

        [Test]
        public void SoundAuthoringEditsCannotChangeCapturedCueBindings()
        {
            configuration.Sounds[0].Clip = null;
            configuration.Sounds = Array.Empty<CombatSoundBinding>();
            Invoke("PresentCue", new CombatPresentationCue(CombatCueKind.Hit, 0));
            Assert.That(root.Presentation.SoundCueCount, Is.EqualTo(1));
            root.Presentation.enabled = false;
            root.Presentation.enabled = true;
            Invoke("PresentCue", new CombatPresentationCue(CombatCueKind.Hit, 0));
            Assert.That(root.Presentation.SoundCueCount, Is.EqualTo(2));
            Assert.That(root.Presentation.GetComponentsInChildren<AudioSource>().Any(source => source.clip == sound), Is.True);
        }

        [Test]
        public void InvalidReplacementPreservesThePreviousActorsAndGraphs()
        {
            root.Presentation.Unbind();
            var player = root.Presentation.PlayerAnimation;
            var enemy = root.Presentation.EnemyAnimation;
            var invalid = new GameObject("Invalid replacement actor");
            try
            {
                configuration.Soldier = invalid;
                Assert.Throws<InvalidOperationException>(() => root.Presentation.RefreshConfiguration(configuration));
                Assert.That(root.Presentation.PlayerAnimation, Is.SameAs(player));
                Assert.That(root.Presentation.EnemyAnimation, Is.SameAs(enemy));
                Assert.That(player.GraphIsValid, Is.True);
                Assert.That(enemy.GraphIsValid, Is.True);
                Assert.That(player.Animator.gameObject.activeInHierarchy, Is.True);
            }
            finally { Object.Destroy(invalid); }
        }

        [Test]
        public void RefreshRejectsBoundEncountersWithoutReplacingGraphs()
        {
            var player = root.Presentation.PlayerAnimation;
            Assert.Throws<InvalidOperationException>(() => root.Presentation.RefreshConfiguration(configuration));
            Assert.That(root.Presentation.PlayerAnimation, Is.SameAs(player));
            Assert.That(player.GraphIsValid, Is.True);
        }

        [UnityTest]
        public IEnumerator DeliberateRefreshCapturesNewRecordsAndReusesOwnedFeedbackResources()
        {
            var old = root.Presentation.PlayerAnimation;
            for (int i = 0; i < configuration.SoldierMotions.Length; i++)
                if (configuration.SoldierMotions[i].Key == "CutRight")
                    configuration.SoldierMotions[i].ContactSeconds = .18;
            Refresh();
            yield return null;
            root.Presentation.PlayerAnimation.Render(new ActorMotion("CutRight", 100000, 700000, 250000), 250000);
            Assert.That(root.Presentation.PlayerAnimation.SampleTimeSeconds, Is.EqualTo(.18).Within(.000001));
            Assert.That(old.GraphIsValid, Is.False);
            Assert.That(root.View.PlayerAnchor.GetComponentsInChildren<Animator>(true), Has.Length.EqualTo(1));
            Assert.That(root.View.EnemyAnchor.GetComponentsInChildren<Animator>(true), Has.Length.EqualTo(1));
            Assert.That(root.Presentation.GetComponentsInChildren<AudioSource>(), Has.Length.EqualTo(4));
            Assert.That(root.Presentation.Effects, Is.Not.Null);
        }

        [Test]
        public void ExactTipPathsRejectMissingMarkersWithoutDiscardingCurrentPresentation()
        {
            root.Presentation.Unbind();
            var player = root.Presentation.PlayerAnimation;
            configuration.SoldierWeaponTipPath = "LOD0/does-not-exist/WeaponTip";
            Assert.Throws<InvalidOperationException>(() => root.Presentation.RefreshConfiguration(configuration));
            Assert.That(root.Presentation.PlayerAnimation, Is.SameAs(player));
            Assert.That(player.GraphIsValid, Is.True);
        }

        private void Refresh()
        {
            root.Presentation.Unbind();
            root.Presentation.RefreshConfiguration(configuration);
            root.Presentation.Bind(root.Encounter, root.Duel);
        }

        private object Private(string name) => typeof(CombatPresentation).GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root.Presentation);

        private void Invoke(string name, params object[] values) => typeof(CombatPresentation).GetMethod(name,
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root.Presentation, values);
    }
}
