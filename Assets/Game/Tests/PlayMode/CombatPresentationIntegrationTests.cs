using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Praxen.Game.Application.Combat;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using Praxen.Game.Presentation.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Praxen.Game.Tests.PlayMode
{
    public sealed class CombatPresentationIntegrationTests
    {
        private GrayboxEncounterRoot root;
        private InputSettings.UpdateMode previousMode;
        private long now;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousMode = InputSystem.settings.updateMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Game/Scenes/CombatPrototype.unity"),
                Is.Not.Null, "Task12 must save a separate playable prototype scene.");
            yield return SceneManager.LoadSceneAsync("CombatPrototype", LoadSceneMode.Single);
            root = Object.FindAnyObjectByType<GrayboxEncounterRoot>();
            root.SetLifecycle(false, true);
            now = 0;
            root.RestartEncounter(() => now);
            yield return GrayboxCapture.WarmFrame(root.EncounterCamera);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) root.Timing.EndEncounter();
            InputSystem.settings.updateMode = previousMode;
            yield return null;
        }

        [Test]
        public void ConfirmedBlockReactsThenReturnsToHeldGuard()
        {
            StepTo(550000);
            now = 600000;
            root.Timing.Session.AdvanceInputFrame(now,
                new[] { Control(1, now, DefenseCommandKind.GuardPress) });
            Render();
            var player = root.Presentation.PlayerAnimation;
            var chest = player.Animator.GetBoneTransform(HumanBodyBones.Chest);
            var braced = chest.localRotation;
            StepTo(700000);
            Assert.That(player.MotionKey, Is.EqualTo("Hit"), "A confirmed block needs a short braced recoil.");
            Assert.That(Quaternion.Angle(braced, chest.localRotation), Is.GreaterThan(.01f));
            Assert.That(root.Encounter.Player.Health, Is.EqualTo(100));
            Assert.That(root.Encounter.Player.Guard, Is.EqualTo(80));
            StepTo(850000);
            Assert.That(player.MotionKey, Is.EqualTo("Guard"));
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);
        }

        [Test]
        public void ImportedCombatantsFaceEachOtherAtTheirEncounterAnchors()
        {
            var player = root.View.PlayerAnchor.GetComponentInChildren<Animator>().transform;
            var enemy = root.View.EnemyAnchor.GetComponentInChildren<Animator>().transform;
            var axis = (enemy.position - player.position).normalized;
            Assert.That(Vector3.Dot(player.forward, axis), Is.GreaterThan(.8f));
            Assert.That(Vector3.Dot(enemy.forward, -axis), Is.GreaterThan(.8f));
        }

        [Test]
        public void ImportedActorsPlayAcceptedDodgeAndIgnoreRejectedButtons()
        {
            var presentation = Presenter;
            var player = Property(presentation, "PlayerAnimation");
            var animator = (Animator)Property(player, "Animator");
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var original = hand.localRotation;
            root.Timing.Session.AdvanceInputFrame(10000, new[] { Control(1, 10000, DefenseCommandKind.DodgeLeft) });
            now = 10000; Render();
            Assert.That(Property(player, "MotionKey"), Is.EqualTo("DodgeLeft"));
            StepTo(110000);
            Assert.That(Quaternion.Angle(original, hand.localRotation), Is.GreaterThan(.01f));
            root.Timing.Session.AdvanceInputFrame(120000, new[] { Control(2, 120000, DefenseCommandKind.DodgeRight) });
            now = 120000; Render();
            Assert.That(Property(player, "MotionKey"), Is.EqualTo("DodgeLeft"));
            Assert.That(root.Encounter.Player.DodgeCharges, Is.EqualTo(2));
            Assert.That(animator.applyRootMotion, Is.False);
        }

        [Test]
        public void PlayerCutSamplesAuthoredContactAtLogicalImpactAndMovesTheRealHand()
        {
            StepTo(1160000);
            var player = Property(Presenter, "PlayerAnimation");
            var animator = (Animator)Property(player, "Animator");
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Gesture(1, 1200000, SwipeDirection.Right);
            var before = hand.localRotation;
            StepTo(1300000);
            Assert.That(Property(player, "MotionKey"), Is.EqualTo("CutRight"));
            Assert.That((double)Property(player, "SampleTimeSeconds"), Is.EqualTo(.1).Within(.002));
            Assert.That(Quaternion.Angle(before, hand.localRotation), Is.GreaterThan(.01f));
            Assert.That(root.Duel.Offense.EnemyHealth, Is.EqualTo(80));
            Assert.That((int)Property(Presenter, "SoundCueCount"), Is.GreaterThan(0));
            Assert.That((int)Property(Property(Presenter, "Effects"), "ImpactCount"), Is.GreaterThan(0));
        }

        [Test]
        public void SuspendAndCountdownFreezePoseCameraAndEffectTime()
        {
            StepTo(550000);
            root.Timing.Session.AdvanceInputFrame(560000, new[] { Control(1, 560000, DefenseCommandKind.DodgeLeft) });
            now = 560000; Render();
            StepTo(660000);
            var player = Property(Presenter, "PlayerAnimation");
            var animator = (Animator)Property(player, "Animator");
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var frozenPose = hand.localRotation;
            var frozenCamera = root.EncounterCamera.transform.position;
            double frozenSample = (double)Property(player, "SampleTimeSeconds");
            root.Timing.Session.Suspend(now, CombatSuspensionReason.FocusLost);
            for (int i = 0; i < 3; i++) { now += 500000; Render(); }
            Assert.That(hand.localRotation, Is.EqualTo(frozenPose));
            Assert.That(root.EncounterCamera.transform.position, Is.EqualTo(frozenCamera));
            Assert.That(Property(player, "SampleTimeSeconds"), Is.EqualTo(frozenSample));
            root.Timing.Session.BeginResume(now, 3000000);
            for (int i = 0; i < 30; i++)
            { now += 50000; root.Timing.Session.AdvanceInputFrame(now, Array.Empty<CombatInput>()); Render(); }
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Countdown));
            Assert.That(hand.localRotation, Is.EqualTo(frozenPose));
            for (int i = 0; i < 30; i++)
            { now += 50000; root.Timing.Session.AdvanceInputFrame(now, Array.Empty<CombatInput>()); Render(); }
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Running));
            Assert.That(root.Timing.Session.Clock.TimeUs, Is.EqualTo(660000));
            Assert.That(hand.localRotation, Is.EqualTo(frozenPose));
            now += 50000; root.Timing.Session.AdvanceInputFrame(now, Array.Empty<CombatInput>()); Render();
            Assert.That((double)Property(player, "SampleTimeSeconds"), Is.GreaterThan(frozenSample));
        }

        [Test]
        public void SuspensionFreezesAnActiveCameraImpulseAndImpactEffect()
        {
            var baseline = root.EncounterCamera.transform.position;
            StepTo(1160000); Gesture(1, 1200000, SwipeDirection.Right); StepTo(1340000);
            var effects = Presenter is CombatPresentation presentation ? presentation.Effects : null;
            Assert.That(effects, Is.Not.Null);
            Assert.That(effects.ActiveImpactCount, Is.GreaterThan(0));
            var frozenCamera = root.EncounterCamera.transform.position;
            Assert.That(frozenCamera, Is.Not.EqualTo(baseline));
            root.Timing.Session.Suspend(now, CombatSuspensionReason.FocusLost);
            for (int i = 0; i < 3; i++) { now += 500000; Render(); }
            Assert.That(root.EncounterCamera.transform.position, Is.EqualTo(frozenCamera));
            Assert.That(effects.RenderedTimeUs, Is.EqualTo(1340000));
            Assert.That(effects.ActiveImpactCount, Is.GreaterThan(0));
        }

        [Test]
        public void ReenableSamplesCurrentActionWithoutReplayingOldImpactsAndRestartRestoresCamera()
        {
            StepTo(1160000); Gesture(1, 1200000, SwipeDirection.Left); StepTo(1300000);
            var presentation = Presenter;
            int cues = (int)Property(presentation, "SoundCueCount");
            ((Behaviour)presentation).enabled = false;
            var baseline = root.EncounterCamera.transform.position;
            StepTo(1450000);
            ((Behaviour)presentation).enabled = true; Render();
            Assert.That(Property(Property(presentation, "PlayerAnimation"), "MotionKey"), Is.EqualTo("CutLeft"));
            Assert.That((double)Property(Property(presentation, "PlayerAnimation"), "SampleTimeSeconds"), Is.GreaterThan(.1));
            Assert.That(Property(presentation, "SoundCueCount"), Is.EqualTo(cues));
            Assert.That(root.EncounterCamera.transform.position, Is.EqualTo(baseline), "Old camera impacts must not replay.");
            var old = root.Timing.Session;
            root.RestartEncounter(() => now);
            Render();
            var position = root.EncounterCamera.transform.position;
            old.AdvanceInputFrame(now + 10000, Array.Empty<CombatInput>());
            Assert.That(root.EncounterCamera.transform.position, Is.EqualTo(position));
            Assert.That((int)Property(Property(presentation, "Effects"), "ActiveImpactCount"), Is.Zero);
        }

        [Test]
        public void ResumeUsesShiftedLiveWarningRatherThanTheOriginalAttackSchedule()
        {
            StepTo(500000);
            root.Timing.Session.Suspend(now, CombatSuspensionReason.FocusLost);
            root.Timing.Session.BeginResume(now, 100000);
            now += 50000; root.Timing.Session.AdvanceInputFrame(now, Array.Empty<CombatInput>()); Render();
            now += 50000; root.Timing.Session.AdvanceInputFrame(now, Array.Empty<CombatInput>()); Render();
            var enemy = Property(Presenter, "EnemyAnimation");
            Assert.That(Property(enemy, "MotionKey"), Is.EqualTo("EnemyTell"));
            Assert.That((double)Property(enemy, "SampleTimeSeconds"), Is.EqualTo(0).Within(.001));
            Assert.That(root.Timing.Session.Timeline.TryGetNextImpact(out var live), Is.True);
            Assert.That(live.TimeUs, Is.EqualTo(1150000));
            for (int i = 0; i < 8; i++)
            { now += 50000; root.Timing.Session.AdvanceInputFrame(now, Array.Empty<CombatInput>()); Render(); }
            Assert.That(root.Timing.Session.Clock.TimeUs, Is.EqualTo(900000));
            Assert.That(Property(enemy, "MotionKey"), Is.EqualTo("EnemyTell"));
            Assert.That(root.Encounter.Player.Health, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator RemovingPresentationComponentDestroysItsOwnedAudioVoices()
        {
            var presentation = (CombatPresentation)Presenter;
            var host = presentation.gameObject;
            var voices = host.GetComponentsInChildren<AudioSource>();
            Assert.That(voices.Length, Is.EqualTo(4));
            var player = presentation.PlayerAnimation;
            root.Timing.EndEncounter();
            Object.Destroy(presentation);
            yield return null;
            yield return null;
            Assert.That(host, Is.Not.Null);
            Assert.That(voices.All(source => source == null), Is.True,
                "Removing the component must remove its owned audio objects.");
            Assert.That(player.GraphIsValid, Is.False);
            Assert.DoesNotThrow(() => root.RestartEncounter(() => now));
            StepWithoutPresentation(100000);
            Assert.That(root.Encounter.Player.Health, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator UnloadDestroysOwnedGraphsAndPreservesTheRefuge()
        {
            var player = Property(Presenter, "PlayerAnimation");
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            Assert.That((bool)Property(player, "GraphIsValid"), Is.False);
            Assert.That(Object.FindObjectsByType<GrayboxEncounterRoot>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator NativePortraitEvidenceShowsAllCutDirectionsAndBracedBlock()
        {
            foreach (var direction in new[] { SwipeDirection.Up, SwipeDirection.Down,
                SwipeDirection.Left, SwipeDirection.Right })
            {
                now = 0;
                root.RestartEncounter(() => now);
                StepTo(1160000);
                Gesture(1, 1200000, direction);
                StepTo(1250000);
                yield return RefugeCapture.WritePortraitEvidence("motion-cut-" + direction + "-windup.png");
                StepTo(1390000);
                yield return RefugeCapture.WritePortraitEvidence("motion-cut-" + direction + "-follow.png");
            }
            now = 0;
            root.RestartEncounter(() => now);
            StepTo(550000);
            now = 600000;
            root.Timing.Session.AdvanceInputFrame(now,
                new[] { Control(1, now, DefenseCommandKind.GuardPress) });
            Render();
            yield return RefugeCapture.WritePortraitEvidence("motion-guard.png");
            StepTo(710000);
            yield return RefugeCapture.WritePortraitEvidence("motion-block-recoil.png");
        }

        [UnityTest]
        public IEnumerator NativeRenderContainsBothImportedHumanoids()
        {
            StepTo(100000);
            Assert.That(root.View.PlayerAnchor.GetComponentsInChildren<SkinnedMeshRenderer>(), Is.Not.Empty);
            Assert.That(root.View.EnemyAnchor.GetComponentsInChildren<SkinnedMeshRenderer>(), Is.Not.Empty);
            yield return GrayboxCapture.WarmFrame(root.EncounterCamera);
            yield return RefugeCapture.WritePortraitEvidence("forever-we-reign-task-12-combat.png");
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        [TestCase(0)]
        public void EnabledAndDisabledPresentationPreserveCompleteAuthoredReplay(int fps)
        {
            var enabled = Replay(fps, true);
            var disabled = Replay(fps, false);
            Assert.That(disabled, Is.EqualTo(enabled));
            Assert.That(enabled, Does.Contain("enemy:1:660000:Parried:100:100"));
            Assert.That(enabled, Does.Contain("attack:1:1300000:20:80"));
            Assert.That(enabled, Does.Contain("attack:2:1800000:20:60"));
            var folder = Environment.GetEnvironmentVariable("PRAXEN_TEST_OUTPUT");
            if (!string.IsNullOrEmpty(folder))
                System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "task12-replay-" + fps + ".json"),
                    JsonUtility.ToJson(new ReplayEvidence { Fps = fps, Enabled = enabled.ToArray(), Disabled = disabled.ToArray() }, true));
        }

        [Serializable]
        private sealed class ReplayEvidence
        { public int Fps; public string[] Enabled; public string[] Disabled; }

        private List<string> Replay(int fps, bool enabled)
        {
            now = 0; root.RestartEncounter(() => now, 20260929);
            ((Behaviour)Presenter).enabled = enabled;
            var session = root.Timing.Session;
            var trace = new List<string>();
            root.Encounter.StrikeResolved += r => trace.Add($"enemy:{r.StrikeId}:{r.TimeUs}:{r.Outcome}:{r.HealthAfter}:{r.GuardAfter}");
            root.Duel.PlayerStrikeResolved += r => trace.Add($"attack:{r.Attack.Id}:{r.TimeUs}:{r.Damage}:{r.EnemyHealth}");
            root.EnemyAI.PatternCommitted += p => trace.Add("pattern:" + p.Pattern.Id + ":" + p.PreparedAtUs + ":" + p.RandomStateAfter);
            var phase = root.Duel.CurrentPhase;
            var commands = new[] {
                new CombatInput(Command(1,600000,SwipeDirection.Up,GestureIntent.Parry,phase)),
                new CombatInput(Command(2,1200000,SwipeDirection.Right,GestureIntent.Attack,new InteractionPhase(phase.Id+1,InteractionPhaseKind.PlayerOpening))),
                new CombatInput(Command(3,1660000,SwipeDirection.Left,GestureIntent.Attack,new InteractionPhase(phase.Id+1,InteractionPhaseKind.PlayerOpening))) };
            int cursor = 0, frame = 0;
            long[] jitter = {11000,49000,7000,81000,23000};
            while (now < 4000000)
            {
                long next = Math.Min(4000000, fps == 0 ? now+jitter[frame%jitter.Length] : (long)Math.Round(++frame*1000000d/fps));
                if (fps == 0) frame++;
                var batch = new List<CombatInput>();
                while (cursor<commands.Length && commands[cursor].SourceTimestampUs<=next) batch.Add(commands[cursor++]);
                now=next; session.AdvanceInputFrame(now,batch); Render();
            }
            trace.Add($"final:{root.Encounter.Player.Health}:{root.Encounter.Player.Guard}:{root.Encounter.Player.DodgeCharges}:{root.Encounter.Player.State}:{root.Duel.Offense.EnemyHealth}:{root.Duel.Momentum.Balance}:{root.Duel.Momentum.Focus}:{root.Duel.CurrentPhase.Kind}:{root.Duel.OpeningRemainingUs}:{root.Duel.Outcome}:{session.RejectedCommandCount}");
            ((Behaviour)Presenter).enabled = true;
            return trace;
        }

        private object Presenter => Property(root, "Presentation");
        private static object Property(object owner, string name)
        {
            Assert.That(owner, Is.Not.Null, name + " owner");
            var property = owner.GetType().GetProperty(name);
            Assert.That(property, Is.Not.Null, name + " presentation observation");
            return property.GetValue(owner);
        }
        private void Render()
        {
            var render = Presenter.GetType().GetMethod("RenderCurrent", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(render, Is.Not.Null); render.Invoke(Presenter, null);
        }
        private void StepTo(long time)
        {
            while (now < time)
            {
                now = Math.Min(time, now + 50000);
                root.Timing.Session.AdvanceInputFrame(now, Array.Empty<CombatInput>()); Render();
            }
        }
        private void StepWithoutPresentation(long time)
        {
            while (now < time)
            {
                now = Math.Min(time, now + 50000);
                root.Timing.Session.AdvanceInputFrame(now, Array.Empty<CombatInput>());
            }
        }
        private void Gesture(long sequence, long time, SwipeDirection direction)
        {
            now = time;
            root.Timing.Session.AdvanceInputFrame(now, new[] { new CombatInput(Command(sequence, time,
                direction, GestureIntent.Attack, root.Duel.CurrentPhase)) }); Render();
        }
        private static CombatInput Control(long sequence,long time,DefenseCommandKind kind) =>
            new CombatInput(new DefenseCommand(sequence,time,kind));
        private static GestureCommand Command(long sequence,long time,SwipeDirection direction,
            GestureIntent intent,InteractionPhase phase) => new GestureCommand(sequence,time,1,direction,intent,phase,
                new NormalizedPoint(.5,.5),new NormalizedPoint(.7,.5));
    }
}
