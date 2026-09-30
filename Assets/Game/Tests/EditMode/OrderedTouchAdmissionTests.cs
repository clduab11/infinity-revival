using System;
using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Application.Combat;
using Praxen.Game.Application.Input;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class OrderedTouchAdmissionTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void RejectedTerminalCannotStrandTheGameplayPointer(bool saturated)
        {
            var session = new CombatSession(0, 6);
            var phases = new GesturePhaseContext();
            var phase = new InteractionPhase(1, InteractionPhaseKind.EnemySequence);
            phases.SetPhase(phase);
            using var encounter = new CombatEncounter(session, phase);
            using var controller = new CombatOpeningController(encounter, phases);
            var recognized = new List<GestureCommand>();
            controller.GestureRecognized += recognized.Add;
            session.AdvanceInputFrame(1000, new[] { Touch(1, SamplePhase.Began, 1000, .1) });
            if (saturated)
            {
                for (int i = 1; i <= 6; i++)
                    session.Timeline.TrySchedule(new CombatMilestone(i, 100000, CombatMilestoneKind.Telegraph));
                session.AdvanceInputFrame(2000, new[] { Touch(1, SamplePhase.Ended, 2000, .4) });
                for (int i = 1; i <= 6; i++) session.Timeline.TryCancelMilestone(i);
            }
            else
            {
                session.AdvanceInputFrame(2000, Array.Empty<CombatInput>());
                session.AdvanceInputFrame(3000, new[] { Touch(1, SamplePhase.Ended, 1500, .4) });
            }
            Assert.That(session.RejectedCommandCount, Is.EqualTo(1));
            session.AdvanceInputFrame(4000, new[] { Touch(2, SamplePhase.Began, 3100, .1),
                Touch(2, SamplePhase.Ended, 3200, .4) });
            Assert.That(recognized, Has.Count.EqualTo(1));
            Assert.That(recognized[0].PointerId, Is.EqualTo(2));
        }

        private static CombatInput Touch(long pointer, SamplePhase phase, long time, double x) =>
            new CombatInput(new OrderedTouchRecord(new TouchSample(pointer, phase,
                new NormalizedPoint(x, .2), time), PointerOwnership.Gameplay, new ScreenMetrics(1000, 1000)));
    }
}
