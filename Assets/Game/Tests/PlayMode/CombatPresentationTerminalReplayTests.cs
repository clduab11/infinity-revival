using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Application.Combat;
using Praxen.Game.Application.Input;
using Praxen.Game.Content.Combat;
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
    public sealed class CombatPresentationTerminalReplayTests
    {
        private enum Scenario { Guard, Dodge, Defeat, Victory }
        private const uint Seed = 20260929;
        private const long RecordingStepUs = 10000;
        private const long RecordingLimitUs = 20000000;
        private static readonly int[] Cadences = { 30, 60, 120, 0 };
        private static readonly long[] JitterUs = { 11000, 49000, 7000, 81000, 23000 };
        private GrayboxEncounterRoot root;
        private InputSettings.UpdateMode previousMode;
        private long now;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousMode = InputSystem.settings.updateMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Game/Scenes/CombatPrototype.unity"),
                Is.Not.Null);
            yield return LoadRoot();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) root.Timing.EndEncounter();
            InputSystem.settings.updateMode = previousMode;
            yield return null;
        }

        [UnityTest]
        public IEnumerator GuardReplayPreservesLiteralBlockAndResources() => Verify(Scenario.Guard);

        [UnityTest]
        public IEnumerator DodgeReplayPreservesLiteralAvoidanceAndCharge() => Verify(Scenario.Dodge);

        [UnityTest]
        public IEnumerator DefeatReplayPreservesTerminalStateAndSamplesDeath() => Verify(Scenario.Defeat);

        [UnityTest]
        public IEnumerator VictoryReplayPreservesTerminalStateAndSamplesDeath() => Verify(Scenario.Victory);

        private IEnumerator LoadRoot()
        {
            yield return SceneManager.LoadSceneAsync("CombatPrototype", LoadSceneMode.Single);
            root = Object.FindAnyObjectByType<GrayboxEncounterRoot>();
            Assert.That(root, Is.Not.Null);
            root.SetLifecycle(false, true);
            now = 0;
            root.RestartEncounter(() => now, Seed, EnemyArchetype.Sword, 0);
            Assert.That(root.Presentation, Is.Not.Null);
        }

        private IEnumerator Verify(Scenario scenario)
        {
            var plan = Record(scenario, root.Duel.CurrentPhase);
            List<string> baseline = null;
            bool first = true;
            foreach (int fps in Cadences)
            {
                if (!first) yield return LoadRoot();
                first = false;
                var enabled = Replay(plan, fps, true);
                AssertLiteral(scenario, enabled);
                if (baseline == null) baseline = enabled.Trace;
                else Assert.That(enabled.Trace, Is.EqualTo(baseline), scenario + " cadence " + fps);
                yield return LoadRoot();
                var disabled = Replay(plan, fps, false);
                AssertLiteral(scenario, disabled);
                Assert.That(disabled.Trace, Is.EqualTo(enabled.Trace), scenario + " presentation " + fps);
                WriteEvidence(scenario, fps, plan, enabled, disabled);
            }
        }

        private static ReplayPlan Record(Scenario scenario, InteractionPhase initialPhase)
        {
            var plan = new ReplayPlan { InitialPhase = initialPhase };
            using (var domain = new RecordingDomain(initialPhase))
            {
                var first = domain.Director.CurrentSchedule[0];
                if (scenario == Scenario.Guard)
                {
                    plan.Commands.Add(Control(1, first.ImpactTimeUs - 60000, DefenseCommandKind.GuardPress));
                    plan.Commands.Add(Control(2, first.ImpactTimeUs + 40000, DefenseCommandKind.GuardRelease));
                }
                else if (scenario == Scenario.Dodge)
                    plan.Commands.Add(Control(1, first.ImpactTimeUs - 100000, DefenseCommandKind.DodgeLeft));
                if (scenario == Scenario.Victory) RecordVictory(domain, plan.Commands);
                else if (scenario == Scenario.Defeat)
                    while (domain.Session.Clock.TimeUs < RecordingLimitUs && domain.Duel.Outcome == DuelOutcome.Running)
                        domain.Session.AdvanceInputFrame(domain.Session.Clock.TimeUs + RecordingStepUs,
                            Array.Empty<CombatInput>());
                if (scenario == Scenario.Victory || scenario == Scenario.Defeat)
                {
                    Assert.That(domain.Duel.Outcome, Is.EqualTo(scenario == Scenario.Victory
                        ? DuelOutcome.Victory : DuelOutcome.Defeat), "Bounded domain recording must reach its literal terminal outcome.");
                    plan.EndUs = domain.Session.Clock.TimeUs + 1000000;
                }
                else plan.EndUs = first.RecoveryTimeUs + RecordingStepUs;
            }
            return plan;
        }

        private static void RecordVictory(RecordingDomain domain, List<CombatInput> commands)
        {
            long sequence = 1;
            while (domain.Session.Clock.TimeUs < RecordingLimitUs && domain.Duel.Outcome == DuelOutcome.Running)
            {
                long next = domain.Session.Clock.TimeUs + RecordingStepUs;
                var input = VictoryInput(domain, sequence, next);
                if (input.HasValue)
                {
                    commands.Add(input.Value);
                    sequence++;
                }
                domain.Session.AdvanceInputFrame(next, input.HasValue
                    ? new[] { input.Value } : Array.Empty<CombatInput>());
            }
        }

        private static CombatInput? VictoryInput(RecordingDomain domain, long sequence, long timeUs)
        {
            var duel = domain.Duel;
            if (duel.CurrentPhase.Kind == InteractionPhaseKind.PlayerOpening)
                return domain.Encounter.Player.State == PlayerCombatState.Ready &&
                    timeUs + duel.Offense.Tuning.WindupUs < duel.Offense.OpeningEndUs
                    ? Gesture(sequence, timeUs, SwipeDirection.Right, GestureIntent.Attack, duel.CurrentPhase)
                    : (CombatInput?)null;
            if (!domain.Session.Timeline.TryGetNextImpact(out var impact) || impact.TimeUs - timeUs != 60000)
                return null;
            foreach (var scheduled in domain.Director.CurrentSchedule)
                if (scheduled.Strike.Id * 3 - 1 == impact.Id)
                    return Gesture(sequence, timeUs, scheduled.Strike.RequiredParryDirection,
                        GestureIntent.Parry, duel.CurrentPhase);
            return null;
        }

        private ReplayResult Replay(ReplayPlan plan, int fps, bool presentationEnabled)
        {
            Assert.That(root.Duel.CurrentPhase.Id, Is.EqualTo(plan.InitialPhase.Id), "Replay phase identities must match the unchanged recording.");
            Assert.That(root.Duel.CurrentPhase.Kind, Is.EqualTo(plan.InitialPhase.Kind));
            root.Presentation.enabled = presentationEnabled;
            var result = new ReplayResult { PresentationEnabled = presentationEnabled };
            Observe(result);
            result.Trace.Add("initial-ai:" + AiState());
            int cursor = 0, frame = 0;
            while (now < plan.EndUs)
            {
                long next = Math.Min(plan.EndUs, fps == 0 ? now + JitterUs[frame % JitterUs.Length]
                    : (long)Math.Round((frame + 1) * 1000000d / fps));
                frame++;
                var batch = new List<CombatInput>();
                while (cursor < plan.Commands.Count && plan.Commands[cursor].SourceTimestampUs <= next)
                    batch.Add(plan.Commands[cursor++]);
                now = next;
                root.Timing.Session.AdvanceInputFrame(now, batch);
                root.Presentation.RenderCurrent();
                CaptureDeath(result);
            }
            Assert.That(cursor, Is.EqualTo(plan.Commands.Count), "Every recorded command must be delivered.");
            CaptureFinal(result);
            return result;
        }

        private void Observe(ReplayResult result)
        {
            root.Encounter.DefenseControlAccepted += (command, time) =>
            {
                result.Controls.Add(command.Kind);
                result.Trace.Add($"accepted-control:{command.Sequence}:{time}:{command.Kind}:" + Resources());
            };
            root.Encounter.ParryAccepted += (command, time) =>
                result.Trace.Add($"accepted-parry:{command.Sequence}:{time}:{command.Direction}:{command.Phase.Id}:" + Resources());
            root.Duel.PlayerAttackStarted += attack =>
                result.Trace.Add($"accepted-attack:{attack.Id}:{attack.StartedUs}:{attack.ImpactUs}:{attack.RecoveryEndUs}:{attack.Direction}:{attack.Phase.Id}:" + Resources());
            root.Encounter.StrikeResolved += resolution =>
            {
                result.Defenses.Add(resolution.Outcome);
                result.Trace.Add($"enemy:{resolution.StrikeId}:{resolution.TimeUs}:{resolution.Outcome}:{resolution.HealthAfter}:{resolution.GuardAfter}:" + Resources());
            };
            root.Duel.PlayerStrikeResolved += resolution =>
            {
                result.AttackHits++;
                result.Trace.Add($"player:{resolution.Attack.Id}:{resolution.TimeUs}:{resolution.Damage}:{resolution.EnemyHealth}:" + Resources());
            };
            root.EnemyAI.PatternCommitted += decision => result.Trace.Add("committed-ai:" + AiState());
            root.Timing.Session.Timeline.Resolved += record =>
            {
                result.Trace.Add($"timeline:{record.TimeUs}:{TimelineIdentity(record)}:" + Resources());
                if (result.TerminalUs < 0 && root.Duel.Outcome != DuelOutcome.Running) result.TerminalUs = record.TimeUs;
            };
        }

        private string Resources()
        {
            var player = root.Encounter.Player;
            var duel = root.Duel;
            return $"{player.Health}:{player.Guard}:{player.DodgeCharges}:{player.State}:{player.GuardHeld}:" +
                $"{duel.Offense.EnemyHealth}:{duel.Momentum.Balance}:{duel.Momentum.Focus}:{duel.Momentum.PendingBalanceBreak}:" +
                $"{duel.CurrentPhase.Id}:{duel.CurrentPhase.Kind}:{duel.Outcome}:{duel.Offense.OpeningStartUs}:{duel.Offense.OpeningEndUs}";
        }

        private string AiState()
        {
            var ai = root.EnemyAI;
            var decision = ai.CurrentDecision;
            string value = $"{ai.Selector.InitialSeed}:{ai.Selector.RandomState}:{ai.Selector.SelectionCount}:" +
                $"{string.Join(",", ai.Selector.History)}:{ai.State}:{ai.ContentRevision}:{ai.BalanceRevision}:" +
                $"{decision.Pattern.Id}:{decision.PreparedAtUs}:{decision.SelectionIndex}:{decision.RandomStateBefore}:{decision.RandomStateAfter}";
            foreach (var scheduled in ai.CurrentSchedule)
                value += $"|{scheduled.Strike.Id}:{scheduled.Strike.AllowedDefenses}:" +
                    $"{scheduled.Strike.SafeDodgeSides}:{scheduled.Strike.RequiredParryDirection}:{scheduled.Strike.GuardCost}:" +
                    $"{scheduled.Strike.HealthDamage}:{scheduled.TelegraphTimeUs}:{scheduled.ImpactTimeUs}:{scheduled.RecoveryTimeUs}";
            return value;
        }

        private static string TimelineIdentity(CombatTimelineEvent record)
        {
            if (record.Milestone.HasValue) return record.Kind + ":" + record.Milestone.Value.Kind + ":" + record.Milestone.Value.Id;
            if (record.Command.HasValue) return record.Kind + ":" + record.Command.Value.Sequence + ":" + record.Command.Value.Intent;
            return record.Kind + ":" + record.DefenseCommand.Value.Sequence + ":" + record.DefenseCommand.Value.Kind;
        }

        private void CaptureDeath(ReplayResult result)
        {
            if (!result.PresentationEnabled || root.Duel.Outcome == DuelOutcome.Running) return;
            var motion = root.Duel.Outcome == DuelOutcome.Defeat ? root.Presentation.PlayerAnimation : root.Presentation.EnemyAnimation;
            Assert.That(motion.MotionKey, Is.EqualTo("Death"));
            result.DeathSample = motion.SampleTimeSeconds;
            if (result.DeathBones == null)
            {
                result.DeathBones = new[] { motion.Animator.GetBoneTransform(HumanBodyBones.Hips),
                    motion.Animator.GetBoneTransform(HumanBodyBones.Spine), motion.Animator.GetBoneTransform(HumanBodyBones.RightHand) };
                result.DeathRotations = new Quaternion[result.DeathBones.Length];
                for (int i = 0; i < result.DeathBones.Length; i++)
                {
                    Assert.That(result.DeathBones[i], Is.Not.Null);
                    result.DeathRotations[i] = result.DeathBones[i].localRotation;
                }
                result.FirstDeathSample = result.DeathSample;
            }
            if (now - result.TerminalUs < 500000 || result.DeathSample <= result.FirstDeathSample) return;
            for (int i = 0; i < result.DeathBones.Length; i++)
                result.DeathPoseChanged |= Quaternion.Angle(result.DeathRotations[i], result.DeathBones[i].localRotation) > .01f;
        }

        private void CaptureFinal(ReplayResult result)
        {
            result.Outcome = root.Duel.Outcome;
            result.Phase = root.Duel.CurrentPhase.Kind;
            result.Health = root.Encounter.Player.Health;
            result.Guard = root.Encounter.Player.Guard;
            result.Dodges = root.Encounter.Player.DodgeCharges;
            result.EnemyHealth = root.Duel.Offense.EnemyHealth;
            result.Ai = root.EnemyAI.State;
            result.Trace.Add($"final:{now}:{root.Timing.Session.Clock.State}:{root.Timing.Session.RejectedCommandCount}:" + Resources());
            result.Trace.Add("current-ai:" + AiState());
            result.Trace.Add("pending:" + root.Timing.Session.Timeline.PendingCount);
        }

        private static void AssertLiteral(Scenario scenario, ReplayResult result)
        {
            Assert.That(result.Trace, Has.Some.StartsWith("initial-ai:20260929:20260929:1:sword.intro:"));
            if (scenario == Scenario.Guard || scenario == Scenario.Dodge)
            {
                Assert.That(result.Outcome, Is.EqualTo(DuelOutcome.Running));
                Assert.That(result.Phase, Is.EqualTo(InteractionPhaseKind.PlayerOpening));
                Assert.That(result.Defenses, Is.EqualTo(new[] { scenario == Scenario.Guard ? DefenseOutcome.Blocked : DefenseOutcome.Dodged }));
                Assert.That(result.Controls, Is.EqualTo(scenario == Scenario.Guard
                    ? new[] { DefenseCommandKind.GuardPress, DefenseCommandKind.GuardRelease } : new[] { DefenseCommandKind.DodgeLeft }));
                Assert.That(result.Health, Is.EqualTo(100));
                Assert.That(result.Guard, Is.EqualTo(scenario == Scenario.Guard ? 80 : 100));
                Assert.That(result.Dodges, Is.EqualTo(scenario == Scenario.Guard ? 3 : 2));
                Assert.That(result.EnemyHealth, Is.EqualTo(100));
                return;
            }
            Assert.That(result.Outcome, Is.EqualTo(scenario == Scenario.Victory ? DuelOutcome.Victory : DuelOutcome.Defeat));
            Assert.That(result.Phase, Is.EqualTo(InteractionPhaseKind.Inactive));
            Assert.That(result.Ai, Is.EqualTo(EnemyDirectorState.Terminal));
            Assert.That(result.Health, Is.EqualTo(scenario == Scenario.Victory ? 100 : 0));
            Assert.That(result.EnemyHealth, Is.EqualTo(scenario == Scenario.Victory ? 0 : 100));
            Assert.That(result.AttackHits, Is.EqualTo(scenario == Scenario.Victory ? 5 : 0));
            Assert.That(result.TerminalUs, Is.GreaterThan(0).And.LessThan(RecordingLimitUs));
            if (!result.PresentationEnabled) return;
            Assert.That(result.DeathSample, Is.GreaterThan(result.FirstDeathSample));
            Assert.That(result.DeathPoseChanged, Is.True, "The terminal Death clip must change real imported humanoid bones.");
        }

        private static void WriteEvidence(Scenario scenario, int fps, ReplayPlan plan, ReplayResult enabled, ReplayResult disabled)
        {
            string folder = Environment.GetEnvironmentVariable("PRAXEN_TEST_OUTPUT");
            if (string.IsNullOrEmpty(folder)) return;
            var commands = new List<string>();
            foreach (var command in plan.Commands)
                commands.Add(command.Gesture.HasValue ? $"{command.SourceTimestampUs}:{command.Gesture.Value.Sequence}:" +
                    $"{command.Gesture.Value.Intent}:{command.Gesture.Value.Direction}:{command.Gesture.Value.Phase.Id}"
                    : $"{command.SourceTimestampUs}:{command.Defense.Value.Sequence}:{command.Defense.Value.Kind}");
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, $"task12-terminal-{scenario}-{fps}.json"),
                JsonUtility.ToJson(new Evidence { Scenario = scenario.ToString(), Fps = fps, EndUs = plan.EndUs,
                    Commands = commands.ToArray(), Enabled = enabled.Trace.ToArray(), Disabled = disabled.Trace.ToArray(),
                    DeathSample = enabled.DeathSample, DeathPoseChanged = enabled.DeathPoseChanged }, true));
        }

        private static CombatInput Control(long sequence, long timeUs, DefenseCommandKind kind) =>
            new CombatInput(new DefenseCommand(sequence, timeUs, kind));

        private static CombatInput Gesture(long sequence, long timeUs, SwipeDirection direction,
            GestureIntent intent, InteractionPhase phase) => new CombatInput(new GestureCommand(sequence,
                timeUs, 1, direction, intent, phase, new NormalizedPoint(.5, .5), new NormalizedPoint(.7, .5)));

        private sealed class ReplayPlan
        {
            public InteractionPhase InitialPhase;
            public long EndUs;
            public readonly List<CombatInput> Commands = new List<CombatInput>();
        }

        private sealed class ReplayResult
        {
            public readonly List<string> Trace = new List<string>();
            public readonly List<DefenseOutcome> Defenses = new List<DefenseOutcome>();
            public readonly List<DefenseCommandKind> Controls = new List<DefenseCommandKind>();
            public DuelOutcome Outcome;
            public InteractionPhaseKind Phase;
            public EnemyDirectorState Ai;
            public int Health, Guard, Dodges, EnemyHealth, AttackHits;
            public long TerminalUs = -1;
            public bool PresentationEnabled, DeathPoseChanged;
            public double FirstDeathSample, DeathSample;
            public Transform[] DeathBones;
            public Quaternion[] DeathRotations;
        }

        [Serializable]
        private sealed class Evidence
        {
            public string Scenario;
            public int Fps;
            public long EndUs;
            public string[] Commands, Enabled, Disabled;
            public double DeathSample;
            public bool DeathPoseChanged;
        }

        private sealed class RecordingDomain : IDisposable
        {
            public readonly CombatSession Session = new CombatSession(0);
            public readonly CombatEncounter Encounter;
            public readonly CombatOpeningController Duel;
            public readonly EnemyPatternDirector Director;

            public RecordingDomain(InteractionPhase phase)
            {
                var phases = new GesturePhaseContext();
                phases.SetPhase(phase);
                Encounter = new CombatEncounter(Session, phase);
                Duel = new CombatOpeningController(Encounter, phases);
                Director = new EnemyPatternDirector(Duel, PrototypeEnemyDecks.Create(EnemyArchetype.Sword), Seed);
                Assert.That(Director.Start(), Is.True);
            }

            public void Dispose()
            {
                Director.Dispose();
                Duel.Dispose();
                Encounter.Dispose();
            }
        }
    }
}
