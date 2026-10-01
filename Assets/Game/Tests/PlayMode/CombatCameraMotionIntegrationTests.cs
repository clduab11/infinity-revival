using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
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
    public sealed class CombatCameraMotionIntegrationTests
    {
        private GrayboxEncounterRoot root;
        private InputSettings.UpdateMode previousMode;
        private CombatPresentationProfile profile;
        private Vector3 baselinePosition;
        private Quaternion baselineRotation;
        private bool previousEnabled;
        private float previousDegrees, previousDistance;
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
            profile = (CombatPresentationProfile)typeof(CombatPresentation).GetField("profile",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root.Presentation);
            baselinePosition = Camera.position;
            baselineRotation = Camera.rotation;
            previousEnabled = Get<bool>("EnableCameraOrbit");
            previousDegrees = Get<float>("CameraOrbitDegrees");
            previousDistance = Get<float>("CameraFocusDistance");
            Set("EnableCameraOrbit", true);
            Set("CameraOrbitDegrees", 3f);
            Set("CameraFocusDistance", 8f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (profile != null && Field("EnableCameraOrbit", false) != null)
            {
                Set("EnableCameraOrbit", previousEnabled);
                Set("CameraOrbitDegrees", previousDegrees);
                Set("CameraFocusDistance", previousDistance);
            }
            if (root != null) root.Timing.EndEncounter();
            InputSystem.settings.updateMode = previousMode;
            yield return null;
        }

        [TestCase(SwipeDirection.Left, 1)]
        [TestCase(SwipeDirection.Right, -1)]
        public void AcceptedCutsOrbitOppositeSidesAndKeepTheFocusPointLocked(SwipeDirection direction, int sign)
        {
            var focus = baselinePosition + baselineRotation * Vector3.forward * 8f;
            var projection = root.EncounterCamera.WorldToViewportPoint(focus);
            StepTo(1160000);
            Attack(1, 1200000, direction);
            StepTo(1250000);
            Assert.That(SignedYaw(), Is.EqualTo(sign * 1.5f).Within(.01f));
            Assert.That(Quaternion.Angle(baselineRotation, Camera.rotation), Is.LessThanOrEqualTo(4.001f));
            Assert.That(root.EncounterCamera.WorldToViewportPoint(focus).x, Is.EqualTo(projection.x).Within(.0001f));
            Assert.That(root.EncounterCamera.WorldToViewportPoint(focus).y, Is.EqualTo(projection.y).Within(.0001f));
            Assert.That(Quaternion.Angle(Quaternion.AngleAxis(SignedYaw(), Vector3.up) * baselineRotation, Camera.rotation), Is.LessThan(.01f));
            StepTo(1700000);
            AssertBaseline();
        }

        [TestCase(DefenseCommandKind.DodgeLeft, 1)]
        [TestCase(DefenseCommandKind.DodgeRight, -1)]
        public void AcceptedDodgesOrbitWithinTheBoundAndReturnExactly(DefenseCommandKind kind, int sign)
        {
            now = 10000;
            root.Timing.Session.AdvanceInputFrame(now, new[] { new CombatInput(new DefenseCommand(1, now, kind)) });
            Render();
            StepTo(190000);
            Assert.That(SignedYaw(), Is.EqualTo(sign * 3f).Within(.01f));
            StepTo(370000);
            AssertBaseline();
        }

        [Test]
        public void OrbitStrengthIsClampedAndZeroOrDisabledProducesNoOrbit()
        {
            Set("CameraOrbitDegrees", 20f);
            StepTo(1160000); Attack(1, 1200000, SwipeDirection.Left); StepTo(1299999);
            Assert.That(Quaternion.Angle(baselineRotation, Camera.rotation), Is.GreaterThan(3.9f));
            Assert.That(Quaternion.Angle(baselineRotation, Camera.rotation), Is.LessThanOrEqualTo(4.001f));
            Set("CameraOrbitDegrees", 0f); Render(); AssertBaseline();
            Set("CameraOrbitDegrees", 3f); Set("EnableCameraOrbit", false); Render(); AssertBaseline();
            Assert.That(root.Duel.Offense.EnemyHealth, Is.EqualTo(100));
        }

        [Test]
        public void OversizedProfileFocusDistanceCannotCreateAnUnboundedOrbit()
        {
            var maximumFocus = baselinePosition + baselineRotation * Vector3.forward * 12f;
            var projection = root.EncounterCamera.WorldToViewportPoint(maximumFocus);
            Set("CameraFocusDistance", 1000f);
            Set("CameraOrbitDegrees", 4f);
            StepTo(1160000); Attack(1, 1200000, SwipeDirection.Left); StepTo(1299999);
            Assert.That(Vector3.Distance(baselinePosition, Camera.position), Is.LessThan(.85f));
            Assert.That(root.EncounterCamera.WorldToViewportPoint(maximumFocus).x,
                Is.EqualTo(projection.x).Within(.0001f));
            Assert.That(root.EncounterCamera.WorldToViewportPoint(maximumFocus).y,
                Is.EqualTo(projection.y).Within(.0001f));
        }

        [Test]
        public void SuspendAndResumeCountdownFreezeBothCameraPositionAndRotation()
        {
            StepTo(1160000); Attack(1, 1200000, SwipeDirection.Right); StepTo(1250000);
            var position = Camera.position;
            var rotation = Camera.rotation;
            Assert.That(Quaternion.Angle(baselineRotation, rotation), Is.GreaterThan(1f));
            root.Timing.Session.Suspend(now, CombatSuspensionReason.FocusLost);
            now += 500000; Render();
            Assert.That(Camera.position, Is.EqualTo(position));
            Assert.That(Camera.rotation, Is.EqualTo(rotation));
            root.Timing.Session.BeginResume(now, 100000);
            now += 50000; root.Timing.Session.AdvanceInputFrame(now, Array.Empty<CombatInput>()); Render();
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Countdown));
            Assert.That(Camera.position, Is.EqualTo(position));
            Assert.That(Camera.rotation, Is.EqualTo(rotation));
            now += 50000; root.Timing.Session.AdvanceInputFrame(now, Array.Empty<CombatInput>()); Render();
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Running));
            Assert.That(Camera.position, Is.EqualTo(position));
            Assert.That(Camera.rotation, Is.EqualTo(rotation));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReenableSuppressesTheCurrentActionUntilTheNextAcceptedAttack(bool startWhileDisabled)
        {
            StepTo(1160000);
            if (startWhileDisabled) root.Presentation.enabled = false;
            Attack(1, 1200000, SwipeDirection.Right); StepTo(1250000);
            if (!startWhileDisabled) root.Presentation.enabled = false;
            StepTo(1450000);
            root.Presentation.enabled = true; Render();
            AssertBaseline();
            Attack(2, 1600000, SwipeDirection.Left); StepTo(1750000);
            Assert.That(SignedYaw(), Is.GreaterThan(1f));
            Assert.That(root.Presentation.PlayerAnimation.MotionKey, Is.EqualTo("CutLeft"));
        }

        [Test]
        public void DisableUnbindAndRestartRestoreTheAuthoredCameraExactly()
        {
            StepTo(1160000); Attack(1, 1200000, SwipeDirection.Left); StepTo(1250000);
            Assert.That(Quaternion.Angle(baselineRotation, Camera.rotation), Is.GreaterThan(1f));
            root.Presentation.enabled = false; AssertBaseline();
            root.Presentation.enabled = true;
            root.RestartEncounter(() => now); AssertBaseline();
            StepCombatBy(1160000); Attack(2, now + 40000, SwipeDirection.Right); StepCombatBy(50000);
            Assert.That(Quaternion.Angle(baselineRotation, Camera.rotation), Is.GreaterThan(1f));
            root.Presentation.Unbind(); AssertBaseline();
        }

        private Transform Camera => root.EncounterCamera.transform;
        private void AssertBaseline()
        {
            Assert.That(Camera.position, Is.EqualTo(baselinePosition));
            Assert.That(Camera.rotation, Is.EqualTo(baselineRotation));
        }
        private float SignedYaw() => Vector3.SignedAngle(
            Vector3.ProjectOnPlane(baselineRotation * Vector3.forward, Vector3.up),
            Vector3.ProjectOnPlane(Camera.forward, Vector3.up), Vector3.up);
        private static FieldInfo Field(string name, bool required = true)
        {
            var field = typeof(CombatPresentationProfile).GetField(name);
            if (required) Assert.That(field, Is.Not.Null, "The profile must expose bounded camera motion settings.");
            return field;
        }
        private T Get<T>(string name) => (T)Field(name).GetValue(profile);
        private void Set(string name, object value)
        {
            Field(name).SetValue(profile, value);
            // These cases exercise live accessibility preferences, not mutations of frozen authoring.
            root.Presentation.ApplyCameraPreferences(profile.EnableCameraOrbit,
                profile.CameraOrbitDegrees, profile.CameraFocusDistance);
        }
        private void Render() => root.Presentation.RenderCurrent();
        private void StepTo(long target)
        {
            while (now < target)
            {
                now = Math.Min(target, now + 50000);
                root.Timing.Session.AdvanceInputFrame(now, Array.Empty<CombatInput>()); Render();
            }
        }
        private void StepCombatBy(long elapsed) => StepTo(now + elapsed);
        private void Attack(long sequence, long deviceTime, SwipeDirection direction)
        {
            now = deviceTime;
            var command = new GestureCommand(sequence, deviceTime, 1,
                direction, GestureIntent.Attack, root.Duel.CurrentPhase,
                new NormalizedPoint(.5, .5), new NormalizedPoint(.7, .5));
            root.Timing.Session.AdvanceInputFrame(now, new[] { new CombatInput(command) }); Render();
        }
    }
}
