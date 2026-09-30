using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Praxen.Game.Application.Combat;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class CombatSessionTests
    {
        private static GestureCommand Command(long sequence, long sourceUs) =>
            new GestureCommand(sequence, sourceUs, 1, SwipeDirection.Right, GestureIntent.Parry,
                new InteractionPhase(1, InteractionPhaseKind.EnemySequence),
                new NormalizedPoint(0.1, 0.2), new NormalizedPoint(0.4, 0.2));

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        [TestCase(0)]
        public void RecordedStreamHasLiteralOutcomeAcrossPresentationRates(int fps)
        {
            const long origin = 5000000;
            var session = new CombatSession(origin);
            var output = new List<string>();
            var defended = new HashSet<long>();
            int hits = 0, parries = 0;
            session.Timeline.Resolved += e =>
            {
                if (e.Kind == CombatEventKind.Command)
                {
                    defended.Add(e.TimeUs);
                    output.Add($"{e.TimeUs}:parry:{e.Command.Value.Sequence}");
                }
                else
                {
                    output.Add($"{e.TimeUs}:{e.Milestone.Value.Kind}");
                    if (e.Milestone.Value.Kind == CombatMilestoneKind.Impact)
                    {
                        if (defended.Contains(e.TimeUs)) parries++;
                        else hits++;
                    }
                }
            };
            session.Timeline.TrySchedule(new CombatMilestone(1, 100000, CombatMilestoneKind.Telegraph));
            session.Timeline.TrySchedule(new CombatMilestone(2, 200000, CombatMilestoneKind.Impact));
            session.Timeline.TrySchedule(new CombatMilestone(3, 200000, CombatMilestoneKind.RecoveryComplete));
            session.Timeline.TrySchedule(new CombatMilestone(4, 400000, CombatMilestoneKind.Impact));
            session.Timeline.TrySchedule(new CombatMilestone(5, 600000, CombatMilestoneKind.Impact));
            var records = new[] { Command(9, origin + 200000), Command(2, origin + 200000),
                Command(3, origin + 399999), Command(4, origin + 600000) };
            int next = 0, frame = 0;
            long now = 0;
            var jitter = new long[] { 11000, 49000, 7000, 81000, 23000 };
            while (now < 700000)
            {
                now = fps == 0 ? now + jitter[frame++ % jitter.Length] :
                    (long)System.Math.Ceiling(++frame * 1000000.0 / fps);
                var batch = new List<GestureCommand>();
                while (next < records.Length && records[next].InputTimestampUs <= origin + now)
                    batch.Add(records[next++]);
                session.AdvanceFrame(origin + now, batch);
            }
            Assert.That(output, Is.EqualTo(new[] { "100000:Telegraph", "200000:parry:9",
                "200000:parry:2", "200000:Impact", "200000:RecoveryComplete",
                "399999:parry:3", "400000:Impact", "600000:parry:4", "600000:Impact" }));
            Assert.That(parries, Is.EqualTo(2));
            Assert.That(hits, Is.EqualTo(1));
            Assert.That(session.RejectedCommandCount, Is.Zero);
            SaveReplayReceipt(fps, output, hits, parries);
        }

        private static void SaveReplayReceipt(int fps, List<string> events, int hits, int parries)
        {
            var directory = System.Environment.GetEnvironmentVariable("PRAXEN_TEST_OUTPUT");
            if (string.IsNullOrEmpty(directory)) return;
            var content = "{\n  \"presentation_fps\": " + fps + ",\n  \"jittered\": " +
                (fps == 0 ? "true" : "false") + ",\n  \"hits\": " + hits +
                ",\n  \"parries\": " + parries + ",\n  \"events\": [\"" +
                string.Join("\", \"", events) + "\"]\n}\n";
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, $"replay-{fps}.json"), content);
        }

        [Test]
        public void StallFreezesBeforeImpactAndResumeRearmsWarningWithoutReplayingInput()
        {
            var session = new CombatSession(1000000);
            var output = new List<CombatTimelineEvent>();
            var warnings = new List<long>();
            session.Timeline.Resolved += output.Add;
            session.IncomingWarningRearmed += warnings.Add;
            session.Timeline.TrySchedule(new CombatMilestone(1, 50000, CombatMilestoneKind.Telegraph));
            session.Timeline.TrySchedule(new CombatMilestone(2, 200000, CombatMilestoneKind.Impact));
            session.AdvanceFrame(1100000, new GestureCommand[0]);
            Assert.That(session.CheckForStall(1300000), Is.True);
            session.AdvanceFrame(1300000, new[] { Command(1, 1200000) });
            Assert.That(output.Select(e => e.Milestone.Value.Kind), Is.EqualTo(new[] { CombatMilestoneKind.Telegraph }));
            Assert.That(session.Clock.TimeUs, Is.EqualTo(100000));
            session.BeginResume(1300000, 100000);
            session.AdvanceFrame(1400000, new[] { Command(2, 1300001) });
            Assert.That(warnings, Is.EqualTo(new long[] { 750000 }));
            Assert.That(session.RejectedCommandCount, Is.EqualTo(2));
            for (long source = 1500000; source <= 2000000; source += 100000)
                session.AdvanceFrame(source, new GestureCommand[0]);
            Assert.That(output, Has.Count.EqualTo(1));
            session.AdvanceFrame(2050000, new GestureCommand[0]);
            Assert.That(output.Last().TimeUs, Is.EqualTo(750000));
            Assert.That(output.Last().Milestone.Value.Kind, Is.EqualTo(CombatMilestoneKind.Impact));
        }

        [Test]
        public void LateAndFutureInputAreRejectedWithoutClamping()
        {
            var session = new CombatSession(1000000);
            session.AdvanceFrame(1050000, new GestureCommand[0]);
            var output = new List<CombatTimelineEvent>();
            session.Timeline.Resolved += output.Add;
            session.AdvanceFrame(1100000, new[] { Command(1, 1050000), Command(2, 1200000), Command(3, 1080000) });
            Assert.That(session.RejectedCommandCount, Is.EqualTo(2));
            Assert.That(output.Single().TimeUs, Is.EqualTo(80000));
        }
    }
}
