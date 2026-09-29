using System;
using NUnit.Framework;
using Praxen.Game.Application;
using Praxen.Game.Infrastructure;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class ApplicationStateTests
    {
        [Test]
        public void NewMachineStartsAtBootstrapWithoutInventingATransition()
        {
            var sink = new BoundedDiagnosticsSink();
            var machine = new ApplicationStateMachine(sink);

            Assert.That(machine.Current, Is.EqualTo(ApplicationState.Bootstrap));
            Assert.That(sink.Count, Is.Zero);
            Assert.That(sink.TotalRecorded, Is.Zero);
        }

        [Test]
        public void ConstructorRequiresADiagnosticsSink()
        {
            Assert.Throws<ArgumentNullException>(() => new ApplicationStateMachine(null));
        }

        [TestCase(ApplicationState.Bootstrap, ApplicationState.Loading)]
        [TestCase(ApplicationState.Bootstrap, ApplicationState.Failed)]
        [TestCase(ApplicationState.Bootstrap, ApplicationState.Shutdown)]
        [TestCase(ApplicationState.Loading, ApplicationState.Refuge)]
        [TestCase(ApplicationState.Loading, ApplicationState.Suspended)]
        [TestCase(ApplicationState.Loading, ApplicationState.Failed)]
        [TestCase(ApplicationState.Loading, ApplicationState.Shutdown)]
        [TestCase(ApplicationState.Refuge, ApplicationState.Suspended)]
        [TestCase(ApplicationState.Refuge, ApplicationState.Failed)]
        [TestCase(ApplicationState.Refuge, ApplicationState.Shutdown)]
        [TestCase(ApplicationState.Suspended, ApplicationState.Refuge)]
        [TestCase(ApplicationState.Suspended, ApplicationState.Failed)]
        [TestCase(ApplicationState.Suspended, ApplicationState.Shutdown)]
        [TestCase(ApplicationState.Failed, ApplicationState.Loading)]
        [TestCase(ApplicationState.Failed, ApplicationState.Shutdown)]
        public void LegalTransitionChangesStateAndRecordsExactlyOneTypedEvent(
            ApplicationState previous, ApplicationState next)
        {
            var sink = new BoundedDiagnosticsSink();
            var machine = CreateAt(previous, sink);
            var before = sink.TotalRecorded;

            Assert.That(machine.TryTransition(next), Is.True);
            Assert.That(machine.Current, Is.EqualTo(next));
            Assert.That(sink.TotalRecorded, Is.EqualTo(before + 1));
            AssertEvent(sink, DiagnosticCode.StateChanged, previous, next);
        }

        [TestCase(ApplicationState.Bootstrap, ApplicationState.Bootstrap)]
        [TestCase(ApplicationState.Bootstrap, ApplicationState.Refuge)]
        [TestCase(ApplicationState.Bootstrap, ApplicationState.Suspended)]
        [TestCase(ApplicationState.Loading, ApplicationState.Bootstrap)]
        [TestCase(ApplicationState.Loading, ApplicationState.Loading)]
        [TestCase(ApplicationState.Refuge, ApplicationState.Bootstrap)]
        [TestCase(ApplicationState.Refuge, ApplicationState.Loading)]
        [TestCase(ApplicationState.Refuge, ApplicationState.Refuge)]
        [TestCase(ApplicationState.Suspended, ApplicationState.Bootstrap)]
        [TestCase(ApplicationState.Suspended, ApplicationState.Loading)]
        [TestCase(ApplicationState.Suspended, ApplicationState.Suspended)]
        [TestCase(ApplicationState.Failed, ApplicationState.Bootstrap)]
        [TestCase(ApplicationState.Failed, ApplicationState.Refuge)]
        [TestCase(ApplicationState.Failed, ApplicationState.Suspended)]
        [TestCase(ApplicationState.Failed, ApplicationState.Failed)]
        [TestCase(ApplicationState.Shutdown, ApplicationState.Bootstrap)]
        [TestCase(ApplicationState.Shutdown, ApplicationState.Loading)]
        [TestCase(ApplicationState.Shutdown, ApplicationState.Refuge)]
        [TestCase(ApplicationState.Shutdown, ApplicationState.Suspended)]
        [TestCase(ApplicationState.Shutdown, ApplicationState.Failed)]
        [TestCase(ApplicationState.Shutdown, ApplicationState.Shutdown)]
        public void IllegalOrDuplicateTransitionKeepsStateAndRecordsRejection(
            ApplicationState current, ApplicationState next)
        {
            var sink = new BoundedDiagnosticsSink();
            var machine = CreateAt(current, sink);
            var before = sink.TotalRecorded;

            Assert.That(machine.TryTransition(next), Is.False);
            Assert.That(machine.Current, Is.EqualTo(current));
            Assert.That(sink.TotalRecorded, Is.EqualTo(before + 1));
            AssertEvent(sink, DiagnosticCode.TransitionRejected, current, current);
        }

        [TestCase(-1)]
        [TestCase(99)]
        public void UndefinedStatesAreRejectedFromEveryState(int invalidValue)
        {
            foreach (ApplicationState current in Enum.GetValues(typeof(ApplicationState)))
            {
                var sink = new BoundedDiagnosticsSink();
                var machine = CreateAt(current, sink);

                Assert.That(machine.TryTransition((ApplicationState)invalidValue), Is.False);
                Assert.That(machine.Current, Is.EqualTo(current));
                AssertEvent(sink, DiagnosticCode.TransitionRejected, current, current);
            }
        }

        [TestCase(ApplicationState.Loading, ApplicationState.Refuge)]
        [TestCase(ApplicationState.Refuge, ApplicationState.Loading)]
        public void SuspensionOnlyResumesToItsCapturedState(
            ApplicationState captured, ApplicationState other)
        {
            var sink = new BoundedDiagnosticsSink();
            var machine = CreateAt(captured, sink);

            Assert.That(machine.TryTransition(ApplicationState.Suspended), Is.True);
            Assert.That(machine.TryTransition(other), Is.False);
            Assert.That(machine.TryTransition(ApplicationState.Suspended), Is.False);
            Assert.That(machine.Current, Is.EqualTo(ApplicationState.Suspended));
            Assert.That(machine.TryTransition(captured), Is.True);
            Assert.That(machine.Current, Is.EqualTo(captured));
        }

        [Test]
        public void FailureWhileSuspendedCanRetryAndCaptureANewResumeState()
        {
            var sink = new BoundedDiagnosticsSink();
            var machine = CreateAt(ApplicationState.Refuge, sink);

            Assert.That(machine.TryTransition(ApplicationState.Suspended), Is.True);
            Assert.That(machine.TryTransition(ApplicationState.Failed), Is.True);
            Assert.That(machine.TryTransition(ApplicationState.Loading), Is.True);
            Assert.That(machine.TryTransition(ApplicationState.Suspended), Is.True);
            Assert.That(machine.TryTransition(ApplicationState.Refuge), Is.False);
            Assert.That(machine.TryTransition(ApplicationState.Loading), Is.True);
            Assert.That(machine.TryTransition(ApplicationState.Refuge), Is.True);
            Assert.That(machine.Current, Is.EqualTo(ApplicationState.Refuge));
        }

        private static ApplicationStateMachine CreateAt(
            ApplicationState state, IDiagnosticsSink sink)
        {
            var machine = new ApplicationStateMachine(sink);
            if (state == ApplicationState.Bootstrap)
                return machine;
            if (state == ApplicationState.Failed || state == ApplicationState.Shutdown)
            {
                Assert.That(machine.TryTransition(state), Is.True);
                return machine;
            }

            Assert.That(machine.TryTransition(ApplicationState.Loading), Is.True);
            if (state == ApplicationState.Loading)
                return machine;
            Assert.That(machine.TryTransition(ApplicationState.Refuge), Is.True);
            if (state == ApplicationState.Suspended)
                Assert.That(machine.TryTransition(ApplicationState.Suspended), Is.True);
            return machine;
        }

        private static void AssertEvent(BoundedDiagnosticsSink sink, DiagnosticCode code,
            ApplicationState previous, ApplicationState current)
        {
            var events = sink.Snapshot();
            var diagnosticEvent = events[events.Length - 1];
            Assert.That(diagnosticEvent.Code, Is.EqualTo(code));
            Assert.That(diagnosticEvent.PreviousState, Is.EqualTo(previous));
            Assert.That(diagnosticEvent.CurrentState, Is.EqualTo(current));
        }
    }
}
