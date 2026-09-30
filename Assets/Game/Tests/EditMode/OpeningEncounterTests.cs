using System;
using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Application.Combat;
using Praxen.Game.Application.Input;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class OpeningEncounterTests
    {
        private const long OpeningStart = 300000;
        private const long OpeningEnd = 2300000;

        [Test]
        public void RecoveryStartsNormalOpeningAndClosureAdvancesBothPhasePorts()
        {
            using var rig = new Rig();
            var ready = new List<long>();
            rig.Controller.EnemySequenceReady += ready.Add;
            Assert.That(rig.Controller.CommitStrike(Strike(1), 0), Is.True);
            rig.AdvanceTo(OpeningStart - 1);
            Assert.That(rig.Controller.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.EnemySequence));
            rig.AdvanceTo(OpeningStart);
            Assert.That(rig.Controller.CurrentPhase.Id, Is.EqualTo(2));
            Assert.That(rig.Controller.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.PlayerOpening));
            Assert.That(rig.Controller.OpeningRemainingUs, Is.EqualTo(2000000));
            Assert.That(rig.Phases.Current.Id, Is.EqualTo(2));
            Assert.That(rig.Encounter.CurrentPhase.Id, Is.EqualTo(2));
            rig.AdvanceTo(OpeningEnd);
            Assert.That(ready, Is.EqualTo(new[] { OpeningEnd }));
            Assert.That(rig.Controller.CurrentPhase.Id, Is.EqualTo(3));
            Assert.That(rig.Controller.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.EnemySequence));
            Assert.That(rig.Controller.OpeningRemainingUs, Is.Zero);
        }

        [Test]
        public void TypedCommandsUseHalfOpenBoundariesAndCannotLeakAcrossPhaseIdentity()
        {
            using var rig = new Rig();
            var strikes = new List<PlayerAttackResolution>();
            rig.Controller.PlayerStrikeResolved += strikes.Add;
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(OpeningStart - 10000);
            rig.FrameTo(OpeningStart + 10000, new[] {
                Attack(1, OpeningStart - 1, 2), Attack(2, OpeningStart, 2),
                Parry(3, OpeningStart, 1) });
            rig.AdvanceTo(OpeningStart + 100000);
            Assert.That(strikes, Has.Count.EqualTo(1));
            Assert.That(strikes[0].Attack.StartedUs, Is.EqualTo(OpeningStart));
            Assert.That(rig.Controller.Momentum.Focus, Is.Zero);
            rig.AdvanceTo(OpeningEnd - 10000);
            rig.FrameTo(OpeningEnd + 10000, new[] { Attack(4, OpeningEnd, 2),
                Attack(5, OpeningEnd + 1, 2) });
            rig.AdvanceTo(OpeningEnd + 200000);
            Assert.That(strikes, Has.Count.EqualTo(1));
            Assert.That(rig.Controller.Offense.EnemyHealth, Is.EqualTo(80));
        }

        [Test]
        public void OrderedContactsStraddlingOpeningStartCancelOldContactAndAcceptNewContact()
        {
            using var rig = new Rig();
            var recognized = new List<GestureCommand>();
            rig.Controller.GestureRecognized += recognized.Add;
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(OpeningStart - 10000);
            rig.FrameInputsTo(OpeningStart + 30000, new[] {
                Touch(1, SamplePhase.Began, OpeningStart - 1, 0.1),
                Touch(1, SamplePhase.Ended, OpeningStart + 1, 0.4),
                Touch(2, SamplePhase.Began, OpeningStart + 2, 0.1),
                Touch(2, SamplePhase.Ended, OpeningStart + 20000, 0.4) });
            rig.AdvanceTo(OpeningStart + 120000);
            Assert.That(recognized, Has.Count.EqualTo(1));
            Assert.That(recognized[0].Intent, Is.EqualTo(GestureIntent.Attack));
            Assert.That(recognized[0].Phase.Id, Is.EqualTo(2));
            Assert.That(rig.Controller.Offense.EnemyHealth, Is.EqualTo(80));
            Assert.That(rig.Session.RejectedCommandCount, Is.Zero);
        }

        [Test]
        public void OrderedContactAtExactOpeningEndCapturesNextEnemyPhase()
        {
            using var rig = new Rig();
            var recognized = new List<GestureCommand>();
            rig.Controller.GestureRecognized += recognized.Add;
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(OpeningEnd - 10000);
            rig.FrameInputsTo(OpeningEnd + 20000, new[] {
                Touch(1, SamplePhase.Began, OpeningEnd - 1, 0.1),
                Touch(1, SamplePhase.Ended, OpeningEnd, 0.4),
                Touch(2, SamplePhase.Began, OpeningEnd, 0.1),
                Touch(2, SamplePhase.Ended, OpeningEnd + 10000, 0.4) });
            Assert.That(recognized, Has.Count.EqualTo(1));
            Assert.That(recognized[0].Intent, Is.EqualTo(GestureIntent.Parry));
            Assert.That(recognized[0].Phase.Id, Is.EqualTo(3));
            Assert.That(rig.Controller.Offense.EnemyHealth, Is.EqualTo(100));
        }

        [TestCase(800000, 2)]
        [TestCase(780000, 1)]
        public void BufferedFirstCommandWinsAndExpiresAtExactBoundary(long bufferedAt, int expectedHits)
        {
            using var rig = new Rig();
            var strikes = new List<PlayerAttackResolution>();
            rig.Controller.PlayerStrikeResolved += strikes.Add;
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(400000, Attack(1, 400000, 2, SwipeDirection.Left));
            rig.AdvanceTo(bufferedAt, Attack(2, bufferedAt, 2, SwipeDirection.Right));
            rig.AdvanceTo(bufferedAt + 10000,
                Attack(3, bufferedAt + 10000, 2, SwipeDirection.Up));
            rig.AdvanceTo(1100000);
            Assert.That(strikes, Has.Count.EqualTo(expectedHits));
            if (expectedHits == 2)
            {
                Assert.That(strikes[1].Attack.Direction, Is.EqualTo(SwipeDirection.Right));
                Assert.That(strikes[1].Attack.StartedUs, Is.EqualTo(900000));
            }
            Assert.That(rig.Controller.Offense.HasBuffer, Is.False);
        }

        [Test]
        public void StrikeAdmissionRequiresAllFourRecordsWithoutPartialBaseCommit()
        {
            using var rig = new Rig(capacity: 3);
            Assert.That(rig.Controller.CommitStrike(Strike(1), 0), Is.False);
            Assert.That(rig.Encounter.HasCommittedStrike, Is.False);
            Assert.That(rig.Session.Timeline.PendingCount, Is.Zero);
            Assert.That(rig.Controller.CurrentPhase.Id, Is.EqualTo(1));
        }

        [Test]
        public void AttackAdmissionRequiresThreeFutureRecordsBeforeMutatingSharedAction()
        {
            using var rig = new Rig(capacity: 4);
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(OpeningStart);
            rig.Session.Timeline.TrySchedule(new CombatMilestone(101, 5000000, CombatMilestoneKind.Telegraph));
            rig.Session.Timeline.TrySchedule(new CombatMilestone(102, 5000001, CombatMilestoneKind.Telegraph));
            rig.AdvanceTo(400000, Attack(1, 400000, 2));
            Assert.That(rig.Encounter.Player.State, Is.EqualTo(PlayerCombatState.Ready));
            Assert.That(rig.Controller.Offense.ActiveAttack.HasValue, Is.False);
            Assert.That(rig.Controller.Offense.EnemyHealth, Is.EqualTo(100));
            Assert.That(rig.Session.Timeline.PendingCount, Is.EqualTo(3));
        }

        [Test]
        public void FirstAttackBuffersThroughInheritedDefenseRecoveryAndStartsAtItsDeadline()
        {
            using var rig = new Rig();
            var strikes = new List<PlayerAttackResolution>();
            rig.Controller.PlayerStrikeResolved += strikes.Add;
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(100000, Parry(1, 100000, 1));
            rig.AdvanceTo(320000, Attack(2, 320000, 2));
            Assert.That(rig.Controller.Offense.HasBuffer, Is.True);
            Assert.That(rig.Controller.Offense.ActiveAttack.HasValue, Is.False);
            rig.AdvanceTo(450000);
            Assert.That(strikes, Has.Count.EqualTo(1));
            Assert.That(strikes[0].Attack.StartedUs, Is.EqualTo(350000));
            Assert.That(strikes[0].TimeUs, Is.EqualTo(450000));
            Assert.That(rig.Controller.Offense.HasBuffer, Is.False);
        }

        [Test]
        public void TypedAttackAtExactRecoveryDeadlineOwnsThreeFutureMilestones()
        {
            using var rig = new Rig();
            var strikes = new List<PlayerAttackResolution>();
            rig.Controller.PlayerStrikeResolved += strikes.Add;
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(400000, Attack(1, 400000, 2));
            rig.AdvanceTo(900000, Attack(2, 900000, 2));
            rig.AdvanceTo(1000000);
            Assert.That(strikes, Has.Count.EqualTo(2));
            Assert.That(strikes[1].Attack.StartedUs, Is.EqualTo(900000));
            Assert.That(strikes[1].TimeUs, Is.EqualTo(1000000));
            Assert.That(rig.Controller.Offense.EnemyHealth, Is.EqualTo(60));
        }

        [Test]
        public void EnemyAdmissionRetriesAfterBoundaryInputBatchReleasesQueueCapacity()
        {
            using var rig = new Rig(capacity: 4);
            var admissions = new List<bool>();
            var boundaries = new List<long>();
            rig.Controller.EnemySequenceReady += time =>
            {
                boundaries.Add(time);
                admissions.Add(rig.Controller.CommitStrike(Strike(2), checked(time + 10000)));
            };
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(600000);
            rig.AdvanceTo(OpeningEnd - 100000);
            rig.FrameInputsTo(OpeningEnd + 4, new[] {
                Touch(1, SamplePhase.Cancelled, OpeningEnd + 1, 0.1),
                Touch(2, SamplePhase.Cancelled, OpeningEnd + 2, 0.1),
                Touch(3, SamplePhase.Cancelled, OpeningEnd + 3, 0.1) });
            Assert.That(admissions, Is.EqualTo(new[] { false, true }));
            Assert.That(boundaries, Is.EqualTo(new[] { OpeningEnd, OpeningEnd + 4 }));
            Assert.That(rig.Encounter.HasCommittedStrike, Is.True);
            Assert.That(rig.Session.Timeline.PendingCount, Is.EqualTo(4));
            Assert.That(rig.Session.RejectedCommandCount, Is.Zero);
        }

        [Test]
        public void ZeroEnemyRecoveryPreservesParryAtInclusiveImpactBeforeStartingOpening()
        {
            using var rig = new Rig();
            var strike = new EnemyStrike(1, DefenseMask.Parry, DodgeSide.None,
                SwipeDirection.Right, 0, 10, 100000, 0);
            rig.Controller.CommitStrike(strike, 0);
            rig.AdvanceTo(100000, Parry(1, 100000, 1));
            Assert.That(rig.Encounter.Player.Health, Is.EqualTo(100));
            Assert.That(rig.Controller.Momentum.Focus, Is.EqualTo(25));
            Assert.That(rig.Controller.Momentum.Balance, Is.EqualTo(25));
            Assert.That(rig.Controller.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.PlayerOpening));
            Assert.That(rig.Controller.Offense.OpeningStartUs, Is.EqualTo(100000));
        }

        [Test]
        public void PlayerDeathWinsTiedPlayerImpactAndCancelsAllOwnedRecords()
        {
            using var rig = new Rig();
            var strikes = new List<PlayerAttackResolution>();
            rig.Controller.PlayerStrikeResolved += strikes.Add;
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(400000, Attack(1, 400000, 2));
            rig.Encounter.Player.ResolveImpact(Strike(2, damage: 100), 500000);
            rig.AdvanceTo(500000);
            Assert.That(rig.Controller.Outcome, Is.EqualTo(DuelOutcome.Defeat));
            Assert.That(rig.Controller.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.Inactive));
            Assert.That(rig.Controller.Offense.EnemyHealth, Is.EqualTo(100));
            Assert.That(strikes, Is.Empty);
            Assert.That(rig.Session.Timeline.PendingCount, Is.Zero);
            Assert.That(rig.Controller.CommitStrike(Strike(3), 600000), Is.False);
        }

        [Test]
        public void EnemyZeroIsTerminalVictoryAndRejectsSubsequentEnemyCommit()
        {
            using var rig = new Rig(tuning: new CombatOffenseTuning(enemyHealth: 20));
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(400000, Attack(1, 400000, 2));
            rig.AdvanceTo(900000);
            Assert.That(rig.Controller.CommitStrike(Strike(2, damage: 100), 900001), Is.False);
            Assert.That(rig.Controller.Outcome, Is.EqualTo(DuelOutcome.Victory));
            Assert.That(rig.Encounter.Player.Health, Is.EqualTo(100));
            Assert.That(rig.Controller.Offense.EnemyHealth, Is.Zero);
            Assert.That(rig.Controller.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.Inactive));
            Assert.That(rig.Session.Timeline.PendingCount, Is.Zero);
        }

        [Test]
        public void OpeningClosureClearsBufferAndPreservesCommittedRecoveryLock()
        {
            using var rig = new Rig(tuning: new CombatOffenseTuning(recoveryUs: 3000000));
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(2100000, Attack(1, 2100000, 2));
            rig.AdvanceTo(2250000, Attack(2, 2250000, 2));
            Assert.That(rig.Controller.Offense.HasBuffer, Is.True);
            rig.AdvanceTo(OpeningEnd);
            Assert.That(rig.Controller.Offense.HasBuffer, Is.False);
            Assert.That(rig.Encounter.Player.State, Is.EqualTo(PlayerCombatState.Recovery));
            Assert.That(rig.Controller.Offense.EnemyHealth, Is.EqualTo(80));
            rig.AdvanceTo(5200000);
            Assert.That(rig.Encounter.Player.State, Is.EqualTo(PlayerCombatState.Ready));
            Assert.That(rig.Controller.Offense.ActiveAttack.HasValue, Is.False);
        }

        [Test]
        public void PauseFreezesOpeningAndCommittedAttackWhileClearingBufferAndContacts()
        {
            using var rig = new Rig();
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(400000, Attack(1, 400000, 2));
            rig.AdvanceTo(450000, Attack(2, 450000, 2));
            var remaining = rig.Controller.OpeningRemainingUs;
            rig.Session.Suspend(rig.DeviceNow, CombatSuspensionReason.ApplicationPaused);
            rig.DeviceNow += 1000000;
            rig.Session.AdvanceFrame(rig.DeviceNow, Array.Empty<GestureCommand>());
            Assert.That(rig.Controller.OpeningRemainingUs, Is.EqualTo(remaining));
            Assert.That(rig.Controller.Offense.HasBuffer, Is.False);
            Assert.That(rig.Controller.Offense.EnemyHealth, Is.EqualTo(100));
            rig.Resume();
            rig.AdvanceTo(500000);
            Assert.That(rig.Controller.Offense.EnemyHealth, Is.EqualTo(80));
            Assert.That(rig.Controller.OpeningRemainingUs, Is.EqualTo(1800000));
            rig.AdvanceTo(1000000);
            Assert.That(rig.Controller.Offense.EnemyHealth, Is.EqualTo(80));
        }

        [Test]
        public void ResumeOpensAtRetimedRecoveryMilestoneRatherThanOriginalAuthoredTime()
        {
            using var rig = new Rig();
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.AdvanceTo(50000);
            rig.Session.Suspend(rig.DeviceNow, CombatSuspensionReason.FocusLost);
            rig.Resume();
            rig.AdvanceTo(OpeningStart);
            Assert.That(rig.Controller.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.EnemySequence));
            rig.AdvanceTo(900000);
            Assert.That(rig.Controller.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.PlayerOpening));
            Assert.That(rig.Controller.Offense.OpeningStartUs, Is.EqualTo(900000));
            Assert.That(rig.Controller.OpeningRemainingUs, Is.EqualTo(2000000));
        }

        [Test]
        public void DefensiveRewardsApplyAtImpactAndFourthParrySelectsBreakOpening()
        {
            using var rig = new Rig();
            for (int index = 0; index < 4; index++)
            {
                long start = index * 2310000L;
                Assert.That(rig.Controller.CommitStrike(Strike(index + 1), start), Is.True);
                rig.AdvanceTo(start + 100000,
                    Parry(index + 1, start + 100000, rig.Controller.CurrentPhase.Id));
                Assert.That(rig.Controller.Momentum.Focus, Is.EqualTo((index + 1) * 25));
                rig.AdvanceTo(start + OpeningStart);
                Assert.That(rig.Controller.OpeningRemainingUs,
                    Is.EqualTo(index == 3 ? 3000000 : 2000000));
                if (index != 3) rig.AdvanceTo(start + OpeningEnd);
            }
            Assert.That(rig.Controller.Momentum.Balance, Is.Zero);
            Assert.That(rig.Controller.Momentum.PendingBalanceBreak, Is.False);
        }

        [Test]
        public void DisposeCancelsOwnedRecordsAndLeavesForeignTimelineRecords()
        {
            using var rig = new Rig();
            rig.Controller.CommitStrike(Strike(1), 0);
            rig.Session.Timeline.TrySchedule(new CombatMilestone(101, 5000000, CombatMilestoneKind.Telegraph));
            rig.Controller.Dispose();
            Assert.That(rig.Session.Timeline.PendingCount, Is.EqualTo(1));
            Assert.That(rig.Controller.CurrentPhase.Kind, Is.EqualTo(InteractionPhaseKind.Inactive));
            Assert.That(() => rig.Controller.CommitStrike(Strike(2), 1),
                Throws.TypeOf<ObjectDisposedException>());
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        [TestCase(0)]
        public void LiteralOffenseReplayMatchesAtThirtySixtyOneTwentyAndJitter(int fps)
        {
            using var rig = new Rig();
            var output = new List<string>();
            rig.Controller.PlayerStrikeResolved += r => output.Add(
                $"{r.TimeUs}:{r.Attack.Direction}:{r.Damage}:{r.EnemyHealth}");
            rig.Controller.EnemySequenceReady += t => output.Add($"{t}:EnemySequence");
            rig.Controller.CommitStrike(Strike(1), 0);
            var inputs = new[] { Parry(1, 100000, 1),
                Attack(2, 400000, 2, SwipeDirection.Left),
                Attack(3, 800000, 2, SwipeDirection.Right),
                Attack(4, 1300000, 2, SwipeDirection.Left), Attack(5, OpeningEnd, 2) };
            int next = 0, frame = 0;
            long now = 0;
            var jitter = new long[] { 11000, 49000, 7000, 81000, 23000 };
            while (now < 2400000)
            {
                now = fps == 0 ? now + jitter[frame++ % jitter.Length] :
                    (long)Math.Ceiling(++frame * 1000000.0 / fps);
                var batch = new List<GestureCommand>();
                while (next < inputs.Length && inputs[next].InputTimestampUs <= Rig.Origin + now)
                    batch.Add(inputs[next++]);
                rig.FrameTo(now, batch);
            }
            TestContext.WriteLine("{\"task\":7,\"replay\":\"opening-offense\",\"presentation_fps\":" +
                fps + ",\"jittered\":" + (fps == 0 ? "true" : "false") +
                ",\"enemy_health\":" + rig.Controller.Offense.EnemyHealth +
                ",\"player_health\":" + rig.Encounter.Player.Health +
                ",\"balance\":" + rig.Controller.Momentum.Balance +
                ",\"focus\":" + rig.Controller.Momentum.Focus +
                ",\"outcome\":\"" + rig.Controller.Outcome + "\",\"events\":[\"" +
                string.Join("\",\"", output) + "\"]}");
            Assert.That(output, Is.EqualTo(new[] { "500000:Left:20:80", "1000000:Right:20:60",
                "1500000:Left:22:38", "2300000:EnemySequence" }));
            Assert.That(rig.Controller.Outcome, Is.EqualTo(DuelOutcome.Running));
            Assert.That(rig.Controller.Momentum.Focus, Is.EqualTo(25));
            Assert.That(rig.Controller.Momentum.Balance, Is.EqualTo(25));
            Assert.That(rig.Session.RejectedCommandCount, Is.Zero);
        }

        private static EnemyStrike Strike(long id, int damage = 0) => new EnemyStrike(id,
            DefenseMask.Parry, DodgeSide.None, SwipeDirection.Right, 0, damage, 100000, 200000);

        private static GestureCommand Attack(long sequence, long time, long phase,
            SwipeDirection direction = SwipeDirection.Right) => Command(sequence, time, phase,
                InteractionPhaseKind.PlayerOpening, GestureIntent.Attack, direction);

        private static GestureCommand Parry(long sequence, long time, long phase) => Command(sequence,
            time, phase, InteractionPhaseKind.EnemySequence, GestureIntent.Parry, SwipeDirection.Right);

        private static GestureCommand Command(long sequence, long time, long phase,
            InteractionPhaseKind kind, GestureIntent intent, SwipeDirection direction) =>
            new GestureCommand(sequence, Rig.Origin + time, sequence, direction, intent,
                new InteractionPhase(phase, kind), new NormalizedPoint(0.1, 0.2),
                new NormalizedPoint(0.4, 0.2));

        private static CombatInput Touch(long pointer, SamplePhase phase, long time, double x) =>
            new CombatInput(new OrderedTouchRecord(new TouchSample(pointer, phase,
                new NormalizedPoint(x, 0.2), Rig.Origin + time), PointerOwnership.Gameplay,
                new ScreenMetrics(1000, 1000)));

        private sealed class Rig : IDisposable
        {
            internal const long Origin = 5000000;
            internal readonly CombatSession Session;
            internal readonly CombatEncounter Encounter;
            internal readonly GesturePhaseContext Phases = new GesturePhaseContext();
            internal readonly CombatOpeningController Controller;
            internal long DeviceNow = Origin;

            internal Rig(int capacity = 1024, CombatOffenseTuning tuning = null)
            {
                Session = new CombatSession(Origin, capacity);
                var phase = new InteractionPhase(1, InteractionPhaseKind.EnemySequence);
                Phases.SetPhase(phase);
                Encounter = new CombatEncounter(Session, phase);
                Controller = new CombatOpeningController(Encounter, Phases, tuning);
            }

            internal void AdvanceTo(long time, GestureCommand? command = null)
            {
                while (time - Session.Clock.TimeUs > 100000)
                    FrameTo(Session.Clock.TimeUs + 100000, Array.Empty<GestureCommand>());
                FrameTo(time, command.HasValue ? new[] { command.Value } : Array.Empty<GestureCommand>());
            }

            internal void FrameTo(long time, IReadOnlyList<GestureCommand> commands)
            {
                DeviceNow += time - Session.Clock.TimeUs;
                Session.AdvanceFrame(DeviceNow, commands);
            }

            internal void FrameInputsTo(long time, IReadOnlyList<CombatInput> inputs)
            {
                DeviceNow += time - Session.Clock.TimeUs;
                Session.AdvanceInputFrame(DeviceNow, inputs);
            }

            internal void Resume()
            {
                Session.BeginResume(DeviceNow, 100000);
                DeviceNow += 100000;
                Session.AdvanceFrame(DeviceNow, Array.Empty<GestureCommand>());
            }

            public void Dispose()
            {
                Controller.Dispose();
                Encounter.Dispose();
            }
        }
    }
}
