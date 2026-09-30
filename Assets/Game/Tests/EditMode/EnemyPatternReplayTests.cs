using System;
using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Application.Combat;
using Praxen.Game.Application.Input;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class EnemyPatternReplayTests
    {
        private static readonly long[] JitterUs = { 11000, 49000, 7000, 81000, 23000 };

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        [TestCase(0)]
        public void SeededDirectorRecordsLiteralPatternsStrikesAndResourcesAtEveryPresentationRate(int fps)
        {
            var session = new CombatSession(0);
            using var encounter = new CombatEncounter(session,
                new InteractionPhase(1, InteractionPhaseKind.EnemySequence));
            using var duel = new CombatOpeningController(encounter, new GesturePhaseContext());
            using var director = new EnemyPatternDirector(duel, Deck(), 1234567);
            var events = Capture(session, encounter, duel, director);
            Assert.That(director.Start(), Is.True);
            Replay(session, fps, 7500000);
            Assert.That(events, Is.EqualTo(new[] {
                "pattern:a:0", "telegraph:1:10000", "strike:1:660000:Hit:100:100:3:0",
                "pattern:c:3160000", "telegraph:2:3170000", "strike:2:3820000:Hit:100:100:3:0",
                "pattern:a:6320000", "telegraph:3:6330000", "strike:3:6980000:Hit:100:100:3:0"
            }));
            Assert.That(director.Selector.SelectionCount, Is.EqualTo(3));
            Assert.That(director.Selector.History, Is.EqualTo(new[] { "c", "a" }));
            Assert.That(director.State, Is.EqualTo(EnemyDirectorState.PlayerOpening));
            Assert.That(duel.Outcome, Is.EqualTo(DuelOutcome.Running));
            TestContext.WriteLine("{\"task\":8,\"replay\":\"seeded-enemy-patterns\"," +
                $"\"presentation_fps\":{fps},\"jittered\":{(fps == 0 ? "true" : "false")}," +
                "\"events\":[\"" + string.Join("\",\"", events) + "\"]," +
                $"\"outcome\":\"{duel.Outcome}\"}}");
        }

        private static List<string> Capture(CombatSession session, CombatEncounter encounter,
            CombatOpeningController duel, EnemyPatternDirector director)
        {
            var events = new List<string>();
            director.PatternCommitted += decision => events.Add(
                $"pattern:{decision.Pattern.Id}:{session.Timeline.TimeUs}");
            encounter.TelegraphStarted += strike => events.Add(
                $"telegraph:{strike.Id}:{session.Timeline.TimeUs}");
            encounter.StrikeResolved += result => events.Add(
                $"strike:{result.StrikeId}:{result.TimeUs}:{result.Outcome}:{encounter.Player.Health}:" +
                $"{encounter.Player.Guard}:{encounter.Player.DodgeCharges}:{duel.Momentum.Focus}");
            return events;
        }

        private static void Replay(CombatSession session, int fps, long finalTimeUs)
        {
            int frame = 0;
            long now = 0;
            while (now < finalTimeUs)
            {
                long candidate = fps == 0 ? now + JitterUs[frame++ % JitterUs.Length] :
                    (long)Math.Ceiling(++frame * 1000000.0 / fps);
                now = Math.Min(candidate, finalTimeUs);
                session.AdvanceFrame(now, Array.Empty<GestureCommand>());
            }
            Assert.That(session.RejectedCommandCount, Is.Zero);
            Assert.That(session.Clock.State, Is.EqualTo(CombatClockState.Running));
            Assert.That(session.Clock.TimeUs, Is.EqualTo(finalTimeUs));
        }

        private static EnemyPatternDeck Deck() => new EnemyPatternDeck("replay", EnemyArchetype.Sword,
            "Replay", "content-test", "balance-test", new[] { Pattern("a"), Pattern("b"), Pattern("c") }, "a");

        private static EnemyPattern Pattern(string id) => new EnemyPattern(id, new[] {
            new EnemyPatternStep(id, DefenseMask.Parry | DefenseMask.Dodge, DodgeSide.Left,
                SwipeDirection.Up, 0, 0) });
    }
}
