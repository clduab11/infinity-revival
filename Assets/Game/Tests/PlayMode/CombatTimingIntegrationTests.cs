using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using Praxen.Game.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using InputPhase = UnityEngine.InputSystem.TouchPhase;

namespace Praxen.Game.Tests.PlayMode
{
    public sealed class CombatTimingIntegrationTests
    {
        private BootstrapCompositionRoot root;
        private Touchscreen device;
        private InputSettings.UpdateMode previousMode;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;
        private long origin, now;
        private readonly List<CombatTimelineEvent> resolved = new List<CombatTimelineEvent>();
        private readonly List<GestureCommand> captured = new List<GestureCommand>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            root = Object.FindFirstObjectByType<BootstrapCompositionRoot>();
            root.SetFocused(true);
            root.SetPaused(false);
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
            root.Timing.Configure(root.InputCapture, root.InteractionPhase, () => now);
            root.InputCapture.CommandProduced += captured.Add;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            root.Timing.EndEncounter();
            if (device != null && device.added) InputSystem.RemoveDevice(device);
            InputSystem.settings.updateMode = previousMode;
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
            resolved.Clear();
            captured.Clear();
            yield return null;
        }

        private void Start()
        {
            root.Timing.BeginEncounter(new InteractionPhase(1, InteractionPhaseKind.EnemySequence));
            root.Timing.Session.Timeline.Resolved += resolved.Add;
        }

        private void Queue(int id, InputPhase phase, double x, long timeUs)
        {
            InputSystem.QueueStateEvent(device, new TouchState { touchId = id, phase = phase,
                position = new Vector2((float)(x * Screen.width), (float)(0.2 * Screen.height)) },
                timeUs / 1000000.0);
        }

        [Test]
        public void RefugeComposesAnInactiveTimingDriver()
        {
            Assert.That(root.Timing.Session, Is.Null);
            Assert.That(root.InteractionPhase.Current.Kind, Is.EqualTo(InteractionPhaseKind.Inactive));
            Assert.That(root.CurrentState, Is.EqualTo(Application.ApplicationState.Refuge));
        }

        [Test]
        public void ReenablingInactiveDriverRestoresRefugeCapture()
        {
            root.InteractionPhase.SetPhase(new InteractionPhase(1, InteractionPhaseKind.EnemySequence));
            root.Timing.enabled = false;
            root.Timing.enabled = true;
            Queue(7, InputPhase.Began, 0.1, origin);
            Queue(7, InputPhase.Moved, 0.4, origin + 2000);
            Queue(7, InputPhase.Ended, 0.4, origin + 4000);
            InputSystem.Update();
            Assert.That(captured, Has.Count.EqualTo(1));
        }

        [Test]
        public void StallInsideInputBatchEmitsSuspensionBeforeAnyImpact()
        {
            int calls = 0, notices = 0;
            root.Timing.Configure(root.InputCapture, root.InteractionPhase,
                () => ++calls < 3 ? origin : origin + 200000);
            Start();
            root.Timing.Suspended += () => notices++;
            root.Timing.Session.Timeline.TrySchedule(new CombatMilestone(1, 100000, CombatMilestoneKind.Impact));
            InputSystem.Update();
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Suspended));
            Assert.That(resolved, Is.Empty);
            Assert.That(notices, Is.EqualTo(1));
        }

        [Test]
        public void RealQueuedGestureResolvesAtItsTimestampBeforeTiedImpact()
        {
            Start();
            root.Timing.Session.Timeline.TrySchedule(new CombatMilestone(1, 2000, CombatMilestoneKind.Impact));
            Queue(7, InputPhase.Began, 0.1, origin);
            Queue(7, InputPhase.Moved, 0.4, origin + 2000);
            Queue(7, InputPhase.Ended, 0.4, origin + 4000);
            now += 8000;
            InputSystem.Update();
            Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Manual));
            Assert.That(captured, Has.Count.EqualTo(1), "Queued player stream must first produce a gesture.");
            Assert.That(resolved, Has.Count.EqualTo(2));
            Assert.That(resolved.Select(e => e.TimeUs), Is.EqualTo(new long[] { 2000, 2000 }));
            Assert.That(resolved[0].Command.Value.InputTimestampUs, Is.EqualTo(origin + 2000));
            Assert.That(resolved[1].Milestone.Value.Kind, Is.EqualTo(CombatMilestoneKind.Impact));
        }

        [Test]
        public void BeforeUpdateStallGatePreventsDelayedBatchRecognitionAndImpact()
        {
            Start();
            root.Timing.Session.Timeline.TrySchedule(new CombatMilestone(1, 100000, CombatMilestoneKind.Impact));
            Queue(7, InputPhase.Began, 0.1, origin + 1000);
            Queue(7, InputPhase.Moved, 0.4, origin + 2000);
            now += 200000;
            InputSystem.Update();
            Assert.That(captured, Is.Empty);
            Assert.That(resolved, Is.Empty);
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Suspended));
            Assert.That(root.Timing.Session.Clock.SuspensionReason, Is.EqualTo(CombatSuspensionReason.FrameStall));
            Assert.That(root.Timing.Session.Clock.TimeUs, Is.Zero);
        }

        [Test]
        public void FocusResumeCancelsHeldGestureAndRearmsTypedWarningAfterCountdown()
        {
            Start();
            var warnings = new List<long>();
            root.Timing.Session.IncomingWarningRearmed += warnings.Add;
            root.Timing.Session.Timeline.TrySchedule(new CombatMilestone(1, 100000, CombatMilestoneKind.Impact));
            Queue(7, InputPhase.Began, 0.1, origin + 1000);
            now += 2000;
            InputSystem.Update();
            now += 1000;
            root.SetFocused(false);
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Suspended));
            now += 100000;
            root.SetFocused(true);
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Countdown));
            for (int i = 0; i < 30; i++) { now += 100000; InputSystem.Update(); }
            Assert.That(warnings, Is.EqualTo(new long[] { 652000 }));
            Queue(7, InputPhase.Moved, 0.4, now + 1000);
            Queue(7, InputPhase.Ended, 0.4, now + 2000);
            now += 3000;
            InputSystem.Update();
            Assert.That(captured, Is.Empty);
            Assert.That(resolved, Is.Empty);
            Queue(8, InputPhase.Began, 0.1, now + 1000);
            Queue(8, InputPhase.Moved, 0.4, now + 2000);
            Queue(8, InputPhase.Ended, 0.4, now + 3000);
            now += 4000;
            InputSystem.Update();
            Assert.That(captured, Has.Count.EqualTo(1));
            Assert.That(resolved.Single().TimeUs, Is.EqualTo(7000));
        }

        [Test]
        public void LifecycleFlagsOnlyResumeAfterBothPermitInput()
        {
            Start();
            root.SetPaused(true);
            root.SetFocused(false);
            root.SetFocused(true);
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Suspended));
            root.SetPaused(false);
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Countdown));
        }

        [Test]
        public void ExplicitStallResumeAndEndUseFreshPhaseIdentities()
        {
            Start();
            now += 150001;
            InputSystem.Update();
            Assert.That(root.Timing.RequestResume(), Is.True);
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Countdown));
            root.Timing.EndEncounter();
            Assert.That(root.InteractionPhase.Current.Id, Is.EqualTo(2));
            Assert.That(root.InteractionPhase.Current.Kind, Is.EqualTo(InteractionPhaseKind.Inactive));
            root.Timing.BeginEncounter(new InteractionPhase(3, InteractionPhaseKind.PlayerOpening));
            Assert.That(root.Timing.Session.Clock.TimeUs, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DynamicInputUpdateAutomaticallyAdvancesComposedTimeline()
        {
            Start();
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            Queue(7, InputPhase.Began, 0.1, origin);
            Queue(7, InputPhase.Moved, 0.4, origin + 2000);
            Queue(7, InputPhase.Ended, 0.4, origin + 4000);
            now += 8000;
            yield return null;
            Assert.That(captured, Has.Count.EqualTo(1), "Dynamic player stream must first produce a gesture.");
            Assert.That(resolved.Single().TimeUs, Is.EqualTo(2000));
        }
    }
}
