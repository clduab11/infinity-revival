using System;
using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Application.Combat;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class DefenseReplayTests
    {
        private static readonly InteractionPhase Phase =
            new InteractionPhase(1, InteractionPhaseKind.EnemySequence);
        private static readonly long[] JitterUs = { 11000, 49000, 7000, 81000, 23000 };

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        [TestCase(0)]
        public void AuthoredDefensesPreserveLiteralResultsAndMixedInputOrderAtEveryPresentationRate(int fps)
        {
            var session = new CombatSession(0);
            using (var encounter = new CombatEncounter(session, Phase))
            {
                var resolutions = CaptureResolutions(encounter);
                var inputs = CaptureInputs(session);
                Commit(encounter, Strike(1, DefenseMask.Guard | DefenseMask.Parry, DodgeSide.None), 0);
                Commit(encounter, Strike(2, DefenseMask.Parry, DodgeSide.None), 1300000);
                Commit(encounter, Strike(3, DefenseMask.Parry | DefenseMask.Dodge, DodgeSide.Left), 2600000);
                Commit(encounter, Strike(4, DefenseMask.Dodge, DodgeSide.Left), 3900000);
                var records = new[] {
                    Control(90, 650000, DefenseCommandKind.GuardPress),
                    Parry(2, 650000, SwipeDirection.Up),
                    Control(40, 1950000, DefenseCommandKind.GuardRelease),
                    Parry(1, 1950000, SwipeDirection.Right),
                    Control(7, 3150000, DefenseCommandKind.DodgeLeft),
                    Parry(3, 3150000, SwipeDirection.Right),
                    Control(6, 4400000, DefenseCommandKind.DodgeRight)
                };

                Replay(session, records, fps, 10400000);

                Assert.That(resolutions, Is.EqualTo(new[] {
                    "1:650000:Blocked:0:20:100:80:False:Guarding:3:0",
                    "2:1950000:Parried:0:0:100:80:False:Recovery:3:2200000",
                    "3:3250000:Dodged:0:0:100:80:False:Dodging:2:3510000",
                    "4:4550000:Hit:25:0:75:80:False:Recovery:1:4750000"
                }));
                Assert.That(inputs, Is.EqualTo(new[] {
                    "650000:650000:90:GuardPress", "650000:650000:2:Up",
                    "1950000:1950000:40:GuardRelease", "1950000:1950000:1:Right",
                    "3150000:3150000:7:DodgeLeft", "3150000:3150000:3:Right",
                    "4400000:4400000:6:DodgeRight"
                }));
                AssertFinal(encounter, 10400000, 75, 80, 3, PlayerCombatState.Ready);
            }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        [TestCase(0)]
        public void GuardBreakStaggerFollowupAndSaturatedDeathHaveLiteralReplayOutcomes(int fps)
        {
            var session = new CombatSession(0);
            using (var encounter = new CombatEncounter(session, Phase))
            {
                var resolutions = CaptureResolutions(encounter);
                var inputs = CaptureInputs(session);
                var telegraphs = new List<long>();
                encounter.TelegraphStarted += strike => telegraphs.Add(strike.Id);
                Commit(encounter, Strike(1, DefenseMask.Guard, DodgeSide.None, 100, 30), 0);
                // This authored followup deliberately impacts inside the 600 ms guard stagger.
                Commit(encounter, Strike(2, DefenseMask.Parry | DefenseMask.Dodge, DodgeSide.Both, 0, 30), 250000);
                Commit(encounter, Strike(3, DefenseMask.Parry, DodgeSide.None, 0, int.MaxValue), 1300000);
                Commit(encounter, Strike(4, DefenseMask.Guard | DefenseMask.Parry, DodgeSide.None), 2600000);
                var records = new[] {
                    Control(90, 630000, DefenseCommandKind.GuardPress),
                    Control(8, 850000, DefenseCommandKind.DodgeRight),
                    Control(2, 900000, DefenseCommandKind.GuardPress),
                    Parry(1, 900000, SwipeDirection.Right),
                    Control(7, 1940000, DefenseCommandKind.GuardPress),
                    Parry(3, 1950000, SwipeDirection.Up),
                    Control(6, 2000000, DefenseCommandKind.GuardPress),
                    Control(5, 3200000, DefenseCommandKind.DodgeLeft),
                    Parry(4, 3250000, SwipeDirection.Right)
                };

                Replay(session, records, fps, 4000000);

                Assert.That(resolutions, Is.EqualTo(new[] {
                    "1:650000:GuardBroken:0:100:100:0:False:Staggered:3:1250000",
                    "2:900000:Hit:30:0:70:0:False:Staggered:3:1250000",
                    "3:1950000:Hit:2147483647:0:0:0:True:Dead:3:0"
                }));
                Assert.That(inputs, Is.EqualTo(new[] {
                    "630000:630000:90:GuardPress", "850000:850000:8:DodgeRight",
                    "900000:900000:2:GuardPress", "900000:900000:1:Right",
                    "1940000:1940000:7:GuardPress", "1950000:1950000:3:Up",
                    "2000000:2000000:6:GuardPress", "3200000:3200000:5:DodgeLeft",
                    "3250000:3250000:4:Right"
                }));
                Assert.That(telegraphs, Is.EqualTo(new long[] { 1, 2, 3 }));
                AssertFinal(encounter, 4000000, 0, 0, 3, PlayerCombatState.Dead);
            }
        }

        private static void Replay(CombatSession session, IReadOnlyList<CombatInput> records,
            int fps, long finalTimeUs)
        {
            int next = 0, frame = 0;
            long now = 0;
            while (now < finalTimeUs)
            {
                long candidate = fps == 0 ? now + JitterUs[frame++ % JitterUs.Length] :
                    (long)Math.Ceiling(++frame * 1000000.0 / fps);
                long nextFrame = Math.Min(candidate, finalTimeUs);
                Assert.That(nextFrame - now, Is.InRange(1L, CombatClock.StallThresholdUs));
                var batch = new List<CombatInput>();
                while (next < records.Count && records[next].SourceTimestampUs <= nextFrame)
                    batch.Add(records[next++]);
                session.AdvanceInputFrame(nextFrame, batch);
                now = nextFrame;
            }
            Assert.That(next, Is.EqualTo(records.Count));
            Assert.That(session.RejectedCommandCount, Is.Zero);
            Assert.That(session.Clock.State, Is.EqualTo(CombatClockState.Running));
            Assert.That(session.Clock.TimeUs, Is.EqualTo(finalTimeUs));
        }

        private static List<string> CaptureResolutions(CombatEncounter encounter)
        {
            var output = new List<string>();
            encounter.StrikeResolved += result => output.Add(
                $"{result.StrikeId}:{result.TimeUs}:{result.Outcome}:{result.HealthDamage}:" +
                $"{result.GuardSpent}:{result.HealthAfter}:{result.GuardAfter}:{result.IsDead}:" +
                $"{encounter.Player.State}:{encounter.Player.DodgeCharges}:{encounter.Player.ActionEndsUs}");
            return output;
        }

        private static List<string> CaptureInputs(CombatSession session)
        {
            var output = new List<string>();
            long previousArrival = 0;
            session.Timeline.Resolved += record =>
            {
                if (!record.Command.HasValue && !record.DefenseCommand.HasValue) return;
                Assert.That(record.ArrivalOrder, Is.GreaterThan(previousArrival));
                previousArrival = record.ArrivalOrder;
                if (record.Command.HasValue)
                {
                    var command = record.Command.Value;
                    output.Add($"{record.TimeUs}:{command.InputTimestampUs}:{command.Sequence}:{command.Direction}");
                }
                else
                {
                    var command = record.DefenseCommand.Value;
                    output.Add($"{record.TimeUs}:{command.InputTimestampUs}:{command.Sequence}:{command.Kind}");
                }
            };
            return output;
        }

        private static void AssertFinal(CombatEncounter encounter, long timeUs,
            int health, int guard, int charges, PlayerCombatState state)
        {
            Assert.That(encounter.Player.TimeUs, Is.EqualTo(timeUs));
            Assert.That(encounter.Player.Health, Is.EqualTo(health));
            Assert.That(encounter.Player.Guard, Is.EqualTo(guard));
            Assert.That(encounter.Player.DodgeCharges, Is.EqualTo(charges));
            Assert.That(encounter.Player.State, Is.EqualTo(state));
            Assert.That(encounter.Player.ActionEndsUs, Is.Zero);
            Assert.That(encounter.Player.GuardHeld, Is.False);
            Assert.That(encounter.Player.DodgeSide, Is.EqualTo(DodgeSide.None));
            Assert.That(encounter.HasCommittedStrike, Is.False);
            Assert.That(encounter.Session.Timeline.PendingCount, Is.Zero);
        }

        private static void Commit(CombatEncounter encounter, EnemyStrike strike, long telegraphUs)
        {
            Assert.That(strike.TelegraphDurationUs, Is.EqualTo(650000));
            Assert.That(strike.RecoveryDurationUs, Is.EqualTo(500000));
            Assert.That(encounter.CommitStrike(strike, telegraphUs), Is.True);
        }

        private static EnemyStrike Strike(long id, DefenseMask mask, DodgeSide sides,
            int guardCost = 20, int damage = 25) =>
            new EnemyStrike(id, mask, sides, SwipeDirection.Right, guardCost, damage);

        private static CombatInput Control(long sequence, long sourceUs, DefenseCommandKind kind) =>
            new CombatInput(new DefenseCommand(sequence, sourceUs, kind));

        private static CombatInput Parry(long sequence, long sourceUs, SwipeDirection direction) =>
            new CombatInput(new GestureCommand(sequence, sourceUs, 1, direction, GestureIntent.Parry,
                Phase, new NormalizedPoint(0.1, 0.2), new NormalizedPoint(0.4, 0.2)));
    }
}
