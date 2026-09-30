using System;
using System.Collections;
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
    public sealed class EnemyPatternInputIntegrationTests
    {
        private GrayboxEncounterRoot root;
        private Touchscreen device;
        private InputSettings.UpdateMode mode;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editorBehavior;
        private long origin, now;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("GrayboxEncounter", LoadSceneMode.Single);
            root = Object.FindFirstObjectByType<GrayboxEncounterRoot>();
            root.SetLifecycle(false, true);
            mode = InputSystem.settings.updateMode;
            background = InputSystem.settings.backgroundBehavior;
            editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            device = InputSystem.AddDevice<Touchscreen>();
            origin = (long)Math.Floor((InputState.currentTime - .01) * 1000000);
            now = origin;
            root.RestartEncounter(() => now);
            yield return GrayboxCapture.WarmFrame(Camera.main);
            Canvas.ForceUpdateCanvases();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) root.Timing.EndEncounter();
            if (device != null && device.added) InputSystem.RemoveDevice(device);
            InputSystem.settings.updateMode = mode;
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
            yield return null;
        }

        [Test]
        public void UnfinishedAndCancelledContactsLeaveCommittedStrikeAndRngUntouched()
        {
            var decision = root.EnemyAI.CurrentDecision;
            var seedState = root.EnemyAI.Selector.RandomState;
            var snapshot = Snapshot();
            Queue(7, InputPhase.Began, .5f, .5f, 100000);
            Queue(7, InputPhase.Moved, .5f, .51f, 110000);
            AdvanceTo(120000);
            AssertCommitment(decision, seedState, snapshot);
            Queue(7, InputPhase.Canceled, .5f, .51f, 130000);
            AdvanceTo(140000);
            AssertCommitment(decision, seedState, snapshot);
        }

        [Test]
        public void CompletedParryRewardsPlayerWithoutChangingCommittedDefinitionOrDeadline()
        {
            var decision = root.EnemyAI.CurrentDecision;
            var state = root.EnemyAI.Selector.RandomState;
            var snapshot = Snapshot();
            AdvanceTo(550000);
            Queue(7, InputPhase.Began, .5f, .5f, 600000);
            Queue(7, InputPhase.Ended, .5f, .65f, 650000);
            AdvanceTo(660000);
            Assert.That(root.Duel.Momentum.Focus, Is.EqualTo(25));
            AssertCommitment(decision, state, snapshot, checkPastMilestones: false);
            Assert.That(root.Timing.Session.Timeline.TryGetMilestone(3, out var recovery), Is.True);
            Assert.That(recovery.TimeUs, Is.EqualTo(1160000));
        }

        [Test]
        public void RestartResetsEncounterSeedHistoryAndFirstTeachingStrike()
        {
            var intro = root.EnemyAI.CurrentDecision.Pattern.Id;
            AdvanceTo(3200000);
            Assert.That(root.EnemyAI.Selector.SelectionCount, Is.EqualTo(2));
            root.RestartEncounter(() => now);
            Assert.That(root.EnemyAI.Selector.SelectionCount, Is.EqualTo(1));
            Assert.That(root.EnemyAI.CurrentDecision.Pattern.Id, Is.EqualTo(intro));
            Assert.That(root.EnemyAI.Selector.History, Is.EqualTo(new[] { intro }));
            Assert.That(root.EnemyAI.CurrentSchedule[0].ImpactTimeUs, Is.EqualTo(660000));
        }

        [Test]
        public void OldPhaseContactCannotAffectNextSeededPattern()
        {
            AdvanceTo(3100000);
            Queue(7, InputPhase.Began, .5f, .5f, 3159000);
            Queue(7, InputPhase.Ended, .65f, .5f, 3161000);
            AdvanceTo(3200000);
            Assert.That(root.EnemyAI.Selector.SelectionCount, Is.EqualTo(2));
            Assert.That(root.Duel.Offense.ActiveAttack.HasValue, Is.False);
            var decision = root.EnemyAI.CurrentDecision;
            var state = root.EnemyAI.Selector.RandomState;
            var snapshot = Snapshot();
            Queue(8, InputPhase.Began, .5f, .5f, 3210000);
            Queue(8, InputPhase.Moved, .51f, .5f, 3220000);
            AdvanceTo(3230000);
            AssertCommitment(decision, state, snapshot);
        }

        private string[] Snapshot() => root.EnemyAI.CurrentSchedule.Select(s =>
            $"{s.Strike.Id}:{s.Strike.AllowedDefenses}:{s.Strike.SafeDodgeSides}:" +
            $"{s.Strike.RequiredParryDirection}:{s.Strike.GuardCost}:{s.Strike.HealthDamage}:" +
            $"{s.TelegraphTimeUs}:{s.ImpactTimeUs}:{s.RecoveryTimeUs}").ToArray();

        private void AssertCommitment(EnemyPatternDecision decision, uint state, string[] snapshot,
            bool checkPastMilestones = true)
        {
            Assert.That(root.EnemyAI.CurrentDecision, Is.SameAs(decision));
            Assert.That(root.EnemyAI.Selector.RandomState, Is.EqualTo(state));
            Assert.That(Snapshot(), Is.EqualTo(snapshot));
            foreach (var s in root.EnemyAI.CurrentSchedule)
            {
                if (!checkPastMilestones) continue;
                // The tell may already have resolved. The future impact and recovery remain authored.
                Assert.That(root.Timing.Session.Timeline.TryGetMilestone(s.Strike.Id * 3 - 1,
                    out var impact), Is.True);
                Assert.That(impact.TimeUs, Is.EqualTo(s.ImpactTimeUs));
                Assert.That(root.Timing.Session.Timeline.TryGetMilestone(s.Strike.Id * 3,
                    out var recovery), Is.True);
                Assert.That(recovery.TimeUs, Is.EqualTo(s.RecoveryTimeUs));
            }
        }

        private void Queue(int id, InputPhase phase, float x, float y, long time) =>
            InputSystem.QueueStateEvent(device, new TouchState { touchId = id, phase = phase,
                position = new Vector2(Screen.width * x, Screen.height * y) },
                (origin + time) / 1000000d);

        private void AdvanceTo(long target)
        {
            while (root.Timing.Session.Clock.TimeUs < target)
            {
                now += Math.Min(100000, target - root.Timing.Session.Clock.TimeUs);
                InputSystem.Update();
            }
            Assert.That(root.Timing.Session.Clock.TimeUs, Is.EqualTo(target));
        }
    }
}
