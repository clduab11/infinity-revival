using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
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
    public sealed class TimestampedOffenseIntegrationTests
    {
        private GrayboxEncounterRoot root;
        private Touchscreen device;
        private InputSettings.UpdateMode previousMode;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;
        private long origin, now;
        private readonly List<GestureCommand> gestures = new List<GestureCommand>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("GrayboxEncounter", LoadSceneMode.Single);
            root = Object.FindAnyObjectByType<GrayboxEncounterRoot>();
            root.SetLifecycle(false, true);
            previousMode = InputSystem.settings.updateMode;
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            device = InputSystem.AddDevice<Touchscreen>();
            origin = (long)Math.Floor((InputState.currentTime - .01) * 1000000);
            now = origin;
            root.RestartEncounter(() => now);
            root.InputCapture.CommandProduced += gestures.Add;
            yield return GrayboxCapture.WarmFrame(Camera.main);
            Canvas.ForceUpdateCanvases();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null)
            {
                root.InputCapture.CommandProduced -= gestures.Add;
                root.Timing.EndEncounter();
            }
            if (device != null && device.added) InputSystem.RemoveDevice(device);
            InputSystem.settings.updateMode = previousMode;
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
            gestures.Clear();
            yield return null;
        }

        [Test]
        public void BatchBeginningAtOpeningBoundaryCapturesNewPhaseAtSourceTime()
        {
            AdvanceTo(1100000);
            Swipe(7, 1160000, 1161000, SwipeDirection.Right);
            AdvanceTo(1200000);
            Assert.That(gestures.Single().Intent, Is.EqualTo(GestureIntent.Attack));
            Assert.That(gestures.Single().InputTimestampUs, Is.EqualTo(origin + 1161000));
            Assert.That(gestures.Single().Phase.Id, Is.EqualTo(root.Duel.CurrentPhase.Id));
            Assert.That(root.Duel.Offense.ActiveAttack.Value.StartedUs, Is.EqualTo(1161000));
            AdvanceTo(1300000);
            Assert.That(root.Duel.Offense.EnemyHealth, Is.EqualTo(80));
        }

        [Test]
        public void ContactBeginningBeforeOpeningCannotBecomeAnAttackInsideSameBatch()
        {
            AdvanceTo(1100000);
            Swipe(7, 1159000, 1161000, SwipeDirection.Right);
            AdvanceTo(1200000);
            Assert.That(gestures, Is.Empty);
            Assert.That(root.Duel.Offense.ActiveAttack.HasValue, Is.False);
            Assert.That(root.Duel.Offense.EnemyHealth, Is.EqualTo(100));
            Assert.That(root.Duel.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.PlayerOpening));
        }

        [Test]
        public void ContactCrossingOpeningEndDoesNotLeakIntoNextEnemySequence()
        {
            AdvanceTo(3100000);
            Swipe(7, 3159000, 3161000, SwipeDirection.Right);
            AdvanceTo(3200000);
            Assert.That(gestures, Is.Empty);
            Assert.That(root.Duel.Offense.EnemyHealth, Is.EqualTo(100));
            Assert.That(root.Duel.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.EnemySequence));
            Assert.That(root.Encounter.HasCommittedStrike, Is.True);
        }

        [Test]
        public void FirstBufferedSwipeWinsAndStartsAtExactRecoveryBoundary()
        {
            AdvanceTo(1150000);
            Swipe(7, 1199000, 1200000, SwipeDirection.Left);
            AdvanceTo(1250000);
            AdvanceTo(1600000);
            Swipe(8, 1659000, 1660000, SwipeDirection.Down);
            Swipe(9, 1669000, 1670000, SwipeDirection.Up);
            AdvanceTo(1690000);
            Assert.That(root.Duel.Offense.HasBuffer, Is.True);
            AdvanceTo(1700000);
            Assert.That(root.Duel.Offense.HasBuffer, Is.False);
            Assert.That(root.Duel.Offense.ActiveAttack.Value.StartedUs, Is.EqualTo(1700000));
            Assert.That(root.Duel.Offense.ActiveAttack.Value.Direction, Is.EqualTo(SwipeDirection.Down));
            AdvanceTo(1800000);
            Assert.That(root.Duel.Offense.EnemyHealth, Is.EqualTo(60));
        }

        [Test]
        public void PauseClearsBufferedSwipeAndRetainsCommittedRecoveryAndOpeningTime()
        {
            AdvanceTo(1150000);
            Swipe(7, 1199000, 1200000, SwipeDirection.Left);
            AdvanceTo(1250000);
            AdvanceTo(1600000);
            Swipe(8, 1659000, 1660000, SwipeDirection.Down);
            AdvanceTo(1670000);
            Assert.That(root.Duel.Offense.HasBuffer, Is.True);
            var remaining = root.Duel.OpeningRemainingUs;
            root.SetLifecycle(true, true);
            Assert.That(root.Duel.Offense.HasBuffer, Is.False);
            AdvanceDeviceBy(500000);
            Assert.That(root.Duel.OpeningRemainingUs, Is.EqualTo(remaining));
            Assert.That(root.Encounter.Player.State, Is.EqualTo(PlayerCombatState.Recovery));
            root.SetLifecycle(false, true);
            AdvanceDeviceBy(3000000);
            AdvanceTo(1700000);
            Assert.That(root.Duel.Offense.ActiveAttack.HasValue, Is.False);
            Assert.That(root.Encounter.Player.State, Is.EqualTo(PlayerCombatState.Ready));
            Assert.That(root.Duel.Offense.EnemyHealth, Is.EqualTo(80));
        }

        [Test]
        public void SuccessfulRawParryAccumulatesBalanceAndFocusBeforeOpening()
        {
            AdvanceTo(550000);
            Swipe(7, 600000, 650000, SwipeDirection.Up);
            AdvanceTo(660000);
            Assert.That(root.Encounter.Player.Health, Is.EqualTo(100));
            Assert.That(root.Duel.Momentum.Balance, Is.EqualTo(25));
            Assert.That(root.Duel.Momentum.Focus, Is.EqualTo(25));
            AdvanceTo(1160000);
            Assert.That(root.Duel.OpeningRemainingUs, Is.EqualTo(2000000));
            Assert.That(root.View.OffenseStatusText, Does.Contain("FOCUS 25"));
        }

        [Test]
        public void InboxDroppingTerminalDoesNotStrandTheNextGameplayContact()
        {
            AdvanceTo(1200000);
            var start = new Vector2(Screen.width * .5f, Screen.height * .5f);
            Queue(7, InputPhase.Began, start, origin + 1200001);
            AdvanceTo(1200002);
            for (int i = 1; i <= 1024; i++)
                Assert.That(root.Timing.SubmitDefense(new DefenseCommand(i, origin + 1201000,
                    DefenseCommandKind.GuardRelease)), Is.True);
            Queue(7, InputPhase.Ended, start, origin + 1201000);
            AdvanceTo(1202000);
            Assert.That(root.Timing.DroppedCommandCount, Is.EqualTo(1));
            Swipe(8, 1210000, 1211000, SwipeDirection.Right);
            AdvanceTo(1220000);
            Assert.That(root.Duel.Offense.ActiveAttack.HasValue, Is.True);
            Assert.That(root.Duel.Offense.ActiveAttack.Value.StartedUs, Is.EqualTo(1211000));
        }

        [Test]
        public void RemovingGameplayDeviceOutsideInputUpdateAllowsFreshContact()
        {
            AdvanceTo(1200000);
            var start = new Vector2(Screen.width * .5f, Screen.height * .5f);
            Queue(7, InputPhase.Began, start, origin + 1200001);
            AdvanceTo(1200002);
            InputSystem.RemoveDevice(device);
            device = InputSystem.AddDevice<Touchscreen>();
            Swipe(8, 1210000, 1211000, SwipeDirection.Right);
            AdvanceTo(1220000);
            Assert.That(root.Duel.Offense.ActiveAttack.HasValue, Is.True);
        }

        private void Swipe(int id, long beginUs, long endUs, SwipeDirection direction)
        {
            var start = new Vector2(Screen.width * .5f, Screen.height * .5f);
            var delta = direction == SwipeDirection.Left ? Vector2.left :
                direction == SwipeDirection.Right ? Vector2.right :
                direction == SwipeDirection.Up ? Vector2.up : Vector2.down;
            Queue(id, InputPhase.Began, start, origin + beginUs);
            Queue(id, InputPhase.Ended, start + delta * Math.Min(Screen.width, Screen.height) * .15f,
                origin + endUs);
        }

        private void Queue(int id, InputPhase phase, Vector2 position, long timeUs) =>
            InputSystem.QueueStateEvent(device, new TouchState { touchId = id, phase = phase,
                position = position }, timeUs / 1000000d);

        private void AdvanceTo(long targetUs)
        {
            long duration = targetUs - root.Timing.Session.Clock.TimeUs;
            Assert.That(duration, Is.GreaterThanOrEqualTo(0));
            AdvanceDeviceBy(duration);
            Assert.That(root.Timing.Session.Clock.TimeUs, Is.EqualTo(targetUs));
        }

        private void AdvanceDeviceBy(long durationUs)
        {
            while (durationUs > 0)
            {
                long step = Math.Min(durationUs, 100000);
                now += step;
                InputSystem.Update();
                durationUs -= step;
            }
        }
    }
}
