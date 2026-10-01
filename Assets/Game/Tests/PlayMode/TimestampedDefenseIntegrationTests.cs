using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Presentation.Combat;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using InputPhase = UnityEngine.InputSystem.TouchPhase;

namespace Praxen.Game.Tests.PlayMode
{
    public sealed class TimestampedDefenseIntegrationTests
    {
        private GrayboxEncounterRoot root;
        private Touchscreen device;
        private InputSettings.UpdateMode previousMode;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;
        private long origin, now;
        private readonly List<CombatTimelineEvent> resolved = new List<CombatTimelineEvent>();
        private readonly List<DefenseResolution> impacts = new List<DefenseResolution>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("GrayboxEncounter", LoadSceneMode.Single);
            root = Object.FindAnyObjectByType<GrayboxEncounterRoot>();
            Assert.That(root, Is.Not.Null);
            root.SetLifecycle(false, true);
            previousMode = InputSystem.settings.updateMode;
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            device = InputSystem.AddDevice<Touchscreen>();
            origin = (long)Math.Floor((InputState.currentTime - 0.01) * 1000000);
            now = origin;
            root.RestartEncounter(() => now);
            root.Timing.Session.Timeline.Resolved += resolved.Add;
            root.Encounter.StrikeResolved += impacts.Add;
            Canvas.ForceUpdateCanvases();
            // A camera-space canvas needs a graphics-backed frame before its raycast depth exists.
            yield return GrayboxCapture.WarmFrame(Camera.main);
            Canvas.ForceUpdateCanvases();
            var p = Center(root.View.GuardZone);
            var sample = new Praxen.Game.Domain.Input.TouchSample(1,
                Praxen.Game.Domain.Input.SamplePhase.Began,
                new Praxen.Game.Domain.Input.NormalizedPoint(p.x / Screen.width, p.y / Screen.height), origin);
            var metrics = new Praxen.Game.Domain.Input.ScreenMetrics(Screen.width, Screen.height);
            var owner = new Praxen.Game.Presentation.Input.EventSystemPointerOwnership().Resolve(in sample, in metrics);
            Assert.That(owner.Kind, Is.EqualTo(Praxen.Game.Domain.Input.PointerOwnerKind.Ui));
            Assert.That(owner.ControlId, Is.EqualTo(EntityId.ToULong(root.View.GuardZone.gameObject.GetEntityId())));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) root.Timing.EndEncounter();
            if (device != null && device.added) InputSystem.RemoveDevice(device);
            InputSystem.settings.updateMode = previousMode;
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
            resolved.Clear();
            impacts.Clear();
            yield return null;
        }

        [Test]
        public void GuardBeginUsesSourceTimestampAndKeepsOwnershipWhenDraggedIntoDodge()
        {
            Queue(7, InputPhase.Began, Center(root.View.GuardZone), origin + 1000);
            AdvanceDeviceBy(2000);

            Assert.That(root.Encounter.Player.GuardHeld, Is.True);
            var press = resolved.Single(value => value.DefenseCommand.HasValue);
            Assert.That(press.TimeUs, Is.EqualTo(1000));
            Assert.That(press.DefenseCommand.Value.InputTimestampUs, Is.EqualTo(origin + 1000));
            Assert.That(press.DefenseCommand.Value.Kind, Is.EqualTo(DefenseCommandKind.GuardPress));
            Queue(7, InputPhase.Moved, Center(root.View.DodgeLeftZone), origin + 3000);
            AdvanceDeviceBy(2000);
            Assert.That(root.Encounter.Player.DodgeCharges, Is.EqualTo(3));
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);

            AdvanceCombatTo(660000);

            Assert.That(impacts.Single().Outcome, Is.EqualTo(DefenseOutcome.Blocked));
            Assert.That(root.Encounter.Player.Health, Is.EqualTo(100));
            Assert.That(root.Encounter.Player.Guard, Is.EqualTo(80));
            Queue(7, InputPhase.Ended, Center(root.View.DodgeLeftZone), origin + 661000);
            AdvanceDeviceBy(2000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.False);
            Assert.That(root.Encounter.Player.DodgeCharges, Is.EqualTo(3));
        }

        [Test]
        public void GuardReleaseAtImpactTimestampResolvesBeforeDamage()
        {
            Queue(7, InputPhase.Began, Center(root.View.GuardZone), origin + 1000);
            AdvanceDeviceBy(2000);
            AdvanceCombatTo(600000);
            Queue(7, InputPhase.Ended, Center(root.View.GuardZone), origin + 660000);

            AdvanceCombatTo(660000);

            var tied = resolved.Where(value => value.TimeUs == 660000).ToArray();
            Assert.That(tied, Has.Length.EqualTo(2));
            Assert.That(tied[0].DefenseCommand.Value.Kind, Is.EqualTo(DefenseCommandKind.GuardRelease));
            Assert.That(tied[0].DefenseCommand.Value.InputTimestampUs, Is.EqualTo(origin + 660000));
            Assert.That(tied[1].Milestone.Value.Kind, Is.EqualTo(CombatMilestoneKind.Impact));
            Assert.That(impacts.Single().Outcome, Is.EqualTo(DefenseOutcome.Hit));
            Assert.That(root.Encounter.Player.Health, Is.EqualTo(75));
            Assert.That(root.Encounter.Player.Guard, Is.EqualTo(100));
        }

        [Test]
        public void SafeLeftDodgeCommitsAtSourceBeginAndAvoidsAuthoredImpact()
        {
            AdvanceCombatTo(550000);
            Queue(7, InputPhase.Began, Center(root.View.DodgeLeftZone), origin + 600000);
            AdvanceCombatTo(610000);

            Assert.That(root.Encounter.Player.DodgeStartedUs, Is.EqualTo(600000));
            Assert.That(root.Encounter.Player.DodgeSide, Is.EqualTo(DodgeSide.Left));
            Assert.That(root.Encounter.Player.DodgeCharges, Is.EqualTo(2));
            Queue(7, InputPhase.Ended, Center(root.View.GuardZone), origin + 615000);
            AdvanceCombatTo(620000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.False);
            Assert.That(root.Encounter.Player.DodgeCharges, Is.EqualTo(2));

            AdvanceCombatTo(660000);

            Assert.That(impacts.Single().Outcome, Is.EqualTo(DefenseOutcome.Dodged));
            Assert.That(impacts.Single().TimeUs, Is.EqualTo(660000));
            Assert.That(root.Encounter.Player.Health, Is.EqualTo(100));
        }

        [Test]
        public void UnsafeRightDodgeSpendsChargeAndTakesFullAuthoredDamage()
        {
            AdvanceCombatTo(550000);
            Queue(7, InputPhase.Began, Center(root.View.DodgeRightZone), origin + 600000);
            AdvanceCombatTo(610000);
            Queue(7, InputPhase.Ended, Center(root.View.DodgeRightZone), origin + 615000);
            AdvanceCombatTo(660000);

            Assert.That(impacts.Single().Outcome, Is.EqualTo(DefenseOutcome.Hit));
            Assert.That(root.Encounter.Player.Health, Is.EqualTo(75));
            Assert.That(root.Encounter.Player.Guard, Is.EqualTo(100));
            Assert.That(root.Encounter.Player.DodgeCharges, Is.EqualTo(2));
            Assert.That(root.Encounter.Player.State, Is.EqualTo(PlayerCombatState.Recovery));
        }

        [Test]
        public void PauseCancelsHeldGuardAndResumeCountdownRequiresFreshBegin()
        {
            Queue(7, InputPhase.Began, Center(root.View.GuardZone), origin + 1000);
            AdvanceDeviceBy(2000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);
            now += 1000;

            root.SetLifecycle(true, true);

            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Suspended));
            Assert.That(root.Encounter.Player.GuardHeld, Is.False);
            Assert.That(root.Encounter.Player.Guard, Is.EqualTo(100));
            Assert.That(root.Encounter.Player.DodgeCharges, Is.EqualTo(3));
            root.SetLifecycle(false, true);
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Countdown));
            AdvanceDeviceBy(3000000);
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Running));
            Queue(7, InputPhase.Moved, Center(root.View.GuardZone), now + 1000);
            Queue(7, InputPhase.Ended, Center(root.View.GuardZone), now + 2000);
            AdvanceDeviceBy(3000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.False);
            Queue(8, InputPhase.Began, Center(root.View.GuardZone), now + 1000);
            AdvanceDeviceBy(2000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);
        }

        [Test]
        public void RemovedDeviceCancelsHeldGuardAndNewDeviceCanAcquireOwnership()
        {
            Queue(7, InputPhase.Began, Center(root.View.GuardZone), origin + 1000);
            AdvanceDeviceBy(2000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);

            InputSystem.RemoveDevice(device);
            AdvanceDeviceBy(20000);

            Assert.That(root.Encounter.Player.GuardHeld, Is.False);
            Assert.That(root.Encounter.Player.DodgeCharges, Is.EqualTo(3));
            device = InputSystem.AddDevice<Touchscreen>();
            Queue(7, InputPhase.Began, Center(root.View.GuardZone), now + 1000);
            AdvanceDeviceBy(2000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);
        }

        [Test]
        public void SecondContactCannotStealControlOrReleaseTheFirstContactsGuard()
        {
            Queue(7, InputPhase.Began, Center(root.View.GuardZone), origin + 1000);
            AdvanceDeviceBy(2000);
            Queue(8, InputPhase.Began, Center(root.View.DodgeLeftZone), origin + 3000);
            AdvanceDeviceBy(2000);

            Assert.That(root.Encounter.Player.DodgeCharges, Is.EqualTo(3));
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);
            Queue(8, InputPhase.Ended, Center(root.View.GuardZone), origin + 5000);
            AdvanceDeviceBy(2000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);
            Queue(7, InputPhase.Ended, Center(root.View.DodgeRightZone), origin + 7000);
            AdvanceDeviceBy(2000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.False);
            Assert.That(resolved.Where(value => value.DefenseCommand.HasValue)
                .Select(value => value.DefenseCommand.Value.Kind),
                Is.EqualTo(new[] { DefenseCommandKind.GuardPress, DefenseCommandKind.GuardRelease }));
        }

        [Test]
        public void FullDriverInboxRetainsTerminalGuardRelease()
        {
            Queue(7, InputPhase.Began, Center(root.View.GuardZone), origin + 1000);
            AdvanceDeviceBy(2000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);
            for (long sequence = 1; sequence <= 1024; sequence++)
                Assert.That(root.Timing.SubmitDefense(new DefenseCommand(sequence, origin + 3000,
                    DefenseCommandKind.GuardPress, guardActionId: 1)), Is.True);
            Queue(7, InputPhase.Ended, Center(root.View.GuardZone), origin + 4000);
            AdvanceDeviceBy(3000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.False);
            Assert.That(root.Timing.DroppedCommandCount, Is.EqualTo(1));
        }

        [Test]
        public void DelayedOldEndCannotReleaseAFreshGuardContact()
        {
            Queue(7, InputPhase.Began, Center(root.View.GuardZone), origin + 1000);
            AdvanceCombatTo(100000);
            Queue(7, InputPhase.Ended, Center(root.View.GuardZone), origin + 50000);
            Queue(8, InputPhase.Began, Center(root.View.GuardZone), origin + 105000);
            AdvanceCombatTo(110000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);
            Assert.That(root.Timing.Session.RejectedCommandCount, Is.EqualTo(1));
            Queue(8, InputPhase.Ended, Center(root.View.GuardZone), origin + 115000);
            AdvanceCombatTo(120000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.False);
        }

        [Test]
        public void DisablingCaptureReleasesGuardAndAllowsFreshControlAfterReenable()
        {
            Queue(7, InputPhase.Began, Center(root.View.GuardZone), origin + 1000);
            AdvanceDeviceBy(2000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);
            root.InputCapture.enabled = false;
            Assert.That(root.Encounter.Player.GuardHeld, Is.False);
            Queue(7, InputPhase.Ended, Center(root.View.GuardZone), origin + 3000);
            AdvanceDeviceBy(2000);
            root.InputCapture.enabled = true;
            Queue(8, InputPhase.Began, Center(root.View.GuardZone), origin + 5000);
            AdvanceDeviceBy(2000);
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);
        }

        [Test]
        public void GameplayBeginCannotBecomeGuardByTravelingIntoItsUiZone()
        {
            var gameplay = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Queue(7, InputPhase.Began, gameplay, origin + 1000);
            AdvanceDeviceBy(2000);
            Queue(7, InputPhase.Moved, Center(root.View.GuardZone), origin + 3000);
            AdvanceDeviceBy(2000);
            Queue(7, InputPhase.Ended, Center(root.View.GuardZone), origin + 5000);
            AdvanceDeviceBy(2000);

            Assert.That(root.Encounter.Player.GuardHeld, Is.False);
            Assert.That(root.Encounter.Player.DodgeCharges, Is.EqualTo(3));
            Assert.That(resolved.Where(value => value.DefenseCommand.HasValue), Is.Empty);
        }

        private static Vector2 Center(RectTransform zone)
        {
            Assert.That(zone, Is.Not.Null);
            return RectTransformUtility.WorldToScreenPoint(Camera.main, zone.TransformPoint(zone.rect.center));
        }

        private void Queue(int id, InputPhase phase, Vector2 position, long timeUs)
        {
            InputSystem.QueueStateEvent(device, new TouchState { touchId = id, phase = phase,
                position = position }, timeUs / 1000000.0);
        }

        private void AdvanceCombatTo(long targetUs)
        {
            long remaining = targetUs - root.Timing.Session.Clock.TimeUs;
            Assert.That(remaining, Is.GreaterThanOrEqualTo(0));
            AdvanceDeviceBy(remaining);
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Running));
            Assert.That(root.Timing.Session.Clock.TimeUs, Is.EqualTo(targetUs));
        }

        private void AdvanceDeviceBy(long durationUs)
        {
            while (durationUs > 0)
            {
                long stepUs = Math.Min(durationUs, 100000);
                now += stepUs;
                InputSystem.Update();
                durationUs -= stepUs;
            }
        }
    }
}
