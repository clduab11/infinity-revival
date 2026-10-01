using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Praxen.Game.Domain.Input;
using Praxen.Game.Presentation;
using Praxen.Game.Presentation.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using InputPhase = UnityEngine.InputSystem.TouchPhase;
using Object = UnityEngine.Object;

namespace Praxen.Game.Tests.PlayMode
{
    public sealed class TouchCaptureTests
    {
        private BootstrapCompositionRoot root;
        private Touchscreen device;
        private InputSettings.UpdateMode previousMode;
        private readonly List<TouchSample> samples = new List<TouchSample>();
        private readonly List<GestureCommand> commands = new List<GestureCommand>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            root = Object.FindAnyObjectByType<BootstrapCompositionRoot>();
            root.SetFocused(true);
            root.SetPaused(false);
            root.InteractionPhase.SetPhase(new InteractionPhase(1, InteractionPhaseKind.EnemySequence));
            root.InputCapture.SampleCaptured += samples.Add;
            root.InputCapture.CommandProduced += commands.Add;
            device = InputSystem.AddDevice<Touchscreen>();
            previousMode = InputSystem.settings.updateMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (device != null && device.added) InputSystem.RemoveDevice(device);
            InputSystem.settings.updateMode = previousMode;
            samples.Clear();
            commands.Clear();
            yield return null;
        }

        private void Queue(int id, InputPhase phase, double x, double y, double time)
        {
            InputSystem.QueueStateEvent(device, new TouchState
            {
                touchId = id, phase = phase,
                position = new Vector2((float)(x * Screen.width), (float)(y * Screen.height))
            }, time);
        }

        private static long Us(double time) => (long)Math.Round(time * 1000000,
            MidpointRounding.AwayFromZero);

        [Test]
        public void QueuedMovesPreserveCrossingSampleAndItsSourceTimestamp()
        {
            var t = Math.Floor((InputState.currentTime - 0.010) * 1000000) / 1000000;
            Queue(7, InputPhase.Began, 0.1, 0.2, t);
            Queue(7, InputPhase.Moved, 0.25, 0.2, t + 0.002);
            Queue(7, InputPhase.Moved, 0.35, 0.2, t + 0.004);
            Queue(7, InputPhase.Moved, 0.1, 0.2, t + 0.006);
            Queue(7, InputPhase.Ended, 0.1, 0.2, t + 0.008);
            InputSystem.Update();
            Assert.That(InputSystem.settings.disableRedundantEventsMerging, Is.True);
            Assert.That(samples, Has.Count.EqualTo(5));
            Assert.That(samples.Select(s => s.TimestampUs), Is.EqualTo(new[]
                { Us(t), Us(t + 0.002), Us(t + 0.004), Us(t + 0.006), Us(t + 0.008) }));
            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That(commands[0].InputTimestampUs, Is.EqualTo(Us(t + 0.002)));
            Assert.That(commands[0].Intent, Is.EqualTo(GestureIntent.Parry));
            Assert.That(commands[0].Direction, Is.EqualTo(SwipeDirection.Right));
        }

        [UnityTest]
        public IEnumerator BeginOnUiRemainsUiOwnedAfterCrossingIntoGameplay()
        {
            var ui = CreateUiBlocker();
            yield return null;
            AssertUiReady(ui);
            var t = InputState.currentTime - 0.004;
            Queue(8, InputPhase.Began, 0.1, 0.2, t);
            Queue(8, InputPhase.Moved, 0.5, 0.2, t + 0.002);
            Queue(8, InputPhase.Ended, 0.5, 0.2, t + 0.003);
            InputSystem.Update();
            Assert.That(samples, Has.Count.EqualTo(3));
            Assert.That(commands, Is.Empty);
            Object.DestroyImmediate(ui);
        }

        [UnityTest]
        public IEnumerator BeginInGameplayRemainsOwnedWhenCrossingIntoUi()
        {
            var ui = CreateUiBlocker();
            yield return null;
            AssertUiReady(ui);
            var t = InputState.currentTime - 0.004;
            Queue(9, InputPhase.Began, 0.5, 0.2, t);
            Queue(9, InputPhase.Moved, 0.1, 0.2, t + 0.002);
            InputSystem.Update();
            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That(commands[0].Direction, Is.EqualTo(SwipeDirection.Left));
            Object.DestroyImmediate(ui);
        }

        [Test]
        public void ChangedPhaseNeverTurnsOldParryIntoAttack()
        {
            var t = InputState.currentTime - 0.004;
            Queue(10, InputPhase.Began, 0.1, 0.2, t);
            InputSystem.Update();
            root.InteractionPhase.SetPhase(new InteractionPhase(2, InteractionPhaseKind.PlayerOpening));
            Queue(10, InputPhase.Moved, 0.4, 0.2, t + 0.002);
            Queue(10, InputPhase.Ended, 0.4, 0.2, t + 0.003);
            InputSystem.Update();
            Assert.That(commands, Is.Empty);
        }

        [Test]
        public void SuspensionDropsHeldGestureAndRequiresReleaseBeforeRestart()
        {
            var t = InputState.currentTime - 0.008;
            Queue(11, InputPhase.Began, 0.1, 0.2, t);
            InputSystem.Update();
            root.SetPaused(true);
            root.SetPaused(false);
            Queue(11, InputPhase.Moved, 0.4, 0.2, t + 0.001);
            Queue(11, InputPhase.Ended, 0.4, 0.2, t + 0.002);
            InputSystem.Update();
            Assert.That(commands, Is.Empty);
            Queue(11, InputPhase.Began, 0.1, 0.2, t + 0.004);
            Queue(11, InputPhase.Moved, 0.4, 0.2, t + 0.005);
            InputSystem.Update();
            Assert.That(commands, Has.Count.EqualTo(1));
        }

        [Test]
        public void DeviceResetCancelsWithoutGeneratingCommand()
        {
            var t = InputState.currentTime - 0.004;
            Queue(12, InputPhase.Began, 0.1, 0.2, t);
            InputSystem.Update();
            InputSystem.ResetDevice(device);
            Queue(12, InputPhase.Moved, 0.4, 0.2, t + 0.002);
            InputSystem.Update();
            Assert.That(commands, Is.Empty);
            Assert.That(samples.Any(s => s.Phase == SamplePhase.Cancelled), Is.True);
        }

        [Test]
        public void RemovedTouchscreenReleasesOwnershipForNewDevice()
        {
            var t = InputState.currentTime - 0.006;
            Queue(31, InputPhase.Began, 0.1, 0.2, t);
            InputSystem.Update();
            InputSystem.RemoveDevice(device);
            device = InputSystem.AddDevice<Touchscreen>();
            Queue(31, InputPhase.Began, 0.1, 0.2, t + 0.002);
            Queue(31, InputPhase.Moved, 0.4, 0.2, t + 0.004);
            InputSystem.Update();
            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That(samples.Any(s => s.Phase == SamplePhase.Cancelled), Is.True);
        }

        [Test]
        public void ReenableWhileHeldCannotManufactureNewGesture()
        {
            var t = InputState.currentTime - 0.004;
            Queue(13, InputPhase.Began, 0.1, 0.2, t);
            InputSystem.Update();
            root.InputCapture.enabled = false;
            root.InputCapture.enabled = true;
            Queue(13, InputPhase.Moved, 0.4, 0.2, t + 0.002);
            InputSystem.Update();
            Assert.That(commands, Is.Empty);
        }

        [Test]
        public void MultipleLeasesRestorePriorMergingOnlyAfterLastRelease()
        {
            root.InputCapture.enabled = false;
            var original = InputSystem.settings.disableRedundantEventsMerging;
            var support = UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled;
            InputSystem.settings.disableRedundantEventsMerging = false;
            var first = new EnhancedTouchSession();
            var second = new EnhancedTouchSession();
            try
            {
                Assert.That(InputSystem.settings.disableRedundantEventsMerging, Is.True);
                first.Dispose();
                first.Dispose();
                Assert.That(InputSystem.settings.disableRedundantEventsMerging, Is.True);
                second.Dispose();
                Assert.That(InputSystem.settings.disableRedundantEventsMerging, Is.False);
                Assert.That(UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled,
                    Is.EqualTo(support));
            }
            finally
            {
                first.Dispose();
                second.Dispose();
                InputSystem.settings.disableRedundantEventsMerging = original;
                root.InputCapture.enabled = true;
            }
        }

        [Test]
        public void SettingsReplacementPreservesSamplesAndRestoresBothObjects()
        {
            root.InputCapture.enabled = false;
            var original = InputSystem.settings;
            var priorFlags = original.hideFlags;
            original.hideFlags = HideFlags.None; // Replacement destroys temporary DontSave settings.
            var priorMerging = original.disableRedundantEventsMerging;
            original.disableRedundantEventsMerging = false;
            var replacement = ScriptableObject.CreateInstance<InputSettings>();
            replacement.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            try
            {
                root.InputCapture.enabled = true;
                InputSystem.settings = replacement;
                Assert.That(replacement.disableRedundantEventsMerging, Is.True);
                QueuedMovesPreserveCrossingSampleAndItsSourceTimestamp();
                root.InputCapture.enabled = false;
                Assert.That(original.disableRedundantEventsMerging, Is.False);
                Assert.That(replacement.disableRedundantEventsMerging, Is.False);
            }
            finally
            {
                root.InputCapture.enabled = false;
                InputSystem.settings = original;
                original.disableRedundantEventsMerging = priorMerging;
                original.hideFlags = priorFlags;
                root.InputCapture.enabled = true;
                Object.DestroyImmediate(replacement);
            }
        }

        [Test]
        public void UpdateModeChangesCancelHeldContactWithoutInventingSamples()
        {
            var t = InputState.currentTime - 0.008;
            Queue(32, InputPhase.Began, 0.1, 0.2, t);
            Queue(32, InputPhase.Moved, 0.12, 0.2, t + 0.001);
            InputSystem.Update();
            Assert.That(samples, Has.Count.EqualTo(2));
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            Assert.That(samples, Has.Count.EqualTo(2), "Settings rebuilds are not source events.");
            Queue(32, InputPhase.Moved, 0.4, 0.2, t + 0.002);
            Queue(32, InputPhase.Ended, 0.4, 0.2, t + 0.003);
            InputSystem.Update();
            Assert.That(commands, Is.Empty);
            Queue(32, InputPhase.Began, 0.1, 0.2, t + 0.004);
            Queue(32, InputPhase.Moved, 0.4, 0.2, t + 0.005);
            InputSystem.Update();
            Assert.That(commands, Has.Count.EqualTo(1));
        }

        [Test]
        public void SecondaryHeldContactIsNotPromotedWhenPrimaryEnds()
        {
            var t = InputState.currentTime - 0.008;
            Queue(21, InputPhase.Began, 0.1, 0.2, t);
            Queue(22, InputPhase.Began, 0.1, 0.3, t + 0.001);
            Queue(21, InputPhase.Ended, 0.1, 0.2, t + 0.002);
            Queue(22, InputPhase.Moved, 0.4, 0.3, t + 0.003);
            Queue(22, InputPhase.Ended, 0.4, 0.3, t + 0.004);
            InputSystem.Update();
            Assert.That(commands, Is.Empty);
            Queue(22, InputPhase.Began, 0.1, 0.3, t + 0.005);
            Queue(22, InputPhase.Moved, 0.4, 0.3, t + 0.006);
            InputSystem.Update();
            Assert.That(commands, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AcceptedCommandShowsShortDirectionTrace()
        {
            var t = InputState.currentTime - 0.004;
            Queue(14, InputPhase.Began, 0.15, 0.25, t);
            Queue(14, InputPhase.Moved, 0.25, 0.25, t + 0.002);
            InputSystem.Update();
            Assert.That(root.GestureTrace.IsVisible, Is.True);
            yield return RefugeCapture.WritePortraitEvidence("gesture-trace.png");
            root.GestureTrace.Clear();
            Assert.That(root.GestureTrace.IsVisible, Is.False);
        }

        private static GameObject CreateUiBlocker()
        {
            Assert.That(EventSystem.current, Is.Not.Null);
            var canvas = new GameObject("OwnershipTestCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var button = new GameObject("OwnedUiControl", typeof(RectTransform), typeof(Image));
            button.transform.SetParent(canvas.transform, false);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0.2f, 1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            return canvas;
        }

        private static void AssertUiReady(GameObject canvas)
        {
            var button = canvas.transform.GetChild(0).gameObject;
            var rect = button.GetComponent<RectTransform>();
            var metrics = new ScreenMetrics(Screen.width, Screen.height);
            var sample = new TouchSample(1, SamplePhase.Began, new NormalizedPoint(0.1, 0.2), 0);
            var resolved = new EventSystemPointerOwnership().Resolve(in sample, in metrics);
            Assert.That(resolved.Kind, Is.EqualTo(PointerOwnerKind.Ui),
                $"UI fixture: screen={Screen.width}x{Screen.height}, canvas={canvas.GetComponent<RectTransform>().rect}, control={rect.rect}, world={rect.position}, depth={button.GetComponent<Image>().depth}");
        }
    }
}
