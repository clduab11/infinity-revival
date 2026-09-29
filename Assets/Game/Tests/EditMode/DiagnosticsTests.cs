using System;
using NUnit.Framework;
using Praxen.Game.Application;
using Praxen.Game.Infrastructure;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class DiagnosticsTests
    {
        [Test]
        public void EmptyBufferReturnsAnEmptySnapshotAndZeroCounts()
        {
            var sink = new BoundedDiagnosticsSink();

            Assert.That(sink.Count, Is.Zero);
            Assert.That(sink.TotalRecorded, Is.Zero);
            Assert.That(sink.Snapshot(), Is.Empty);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void CapacityMustBePositive(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoundedDiagnosticsSink(capacity));
        }

        [Test]
        public void DefaultCapacityRetainsTheLatest128Events()
        {
            var sink = new BoundedDiagnosticsSink();
            var first = Event(ApplicationState.Loading);
            sink.Record(in first);
            for (var i = 0; i < 128; i++)
            {
                var next = Event(ApplicationState.Refuge);
                sink.Record(in next);
            }

            Assert.That(sink.Count, Is.EqualTo(128));
            Assert.That(sink.TotalRecorded, Is.EqualTo(129));
            foreach (var recorded in sink.Snapshot())
                Assert.That(recorded.CurrentState, Is.EqualTo(ApplicationState.Refuge));
        }

        [Test]
        public void OverflowRetainsTheLatestEventsInChronologicalOrderAcrossWraps()
        {
            var sink = new BoundedDiagnosticsSink(3);
            Record(sink, ApplicationState.Loading);
            Record(sink, ApplicationState.Refuge);
            Record(sink, ApplicationState.Suspended);
            Record(sink, ApplicationState.Loading);
            Record(sink, ApplicationState.Failed);
            Record(sink, ApplicationState.Loading);
            Record(sink, ApplicationState.Shutdown);

            var snapshot = sink.Snapshot();
            Assert.That(sink.Count, Is.EqualTo(3));
            Assert.That(sink.TotalRecorded, Is.EqualTo(7));
            Assert.That(snapshot[0].CurrentState, Is.EqualTo(ApplicationState.Failed));
            Assert.That(snapshot[1].CurrentState, Is.EqualTo(ApplicationState.Loading));
            Assert.That(snapshot[2].CurrentState, Is.EqualTo(ApplicationState.Shutdown));
        }

        [Test]
        public void SingleSlotBufferAlwaysContainsTheMostRecentEvent()
        {
            var sink = new BoundedDiagnosticsSink(1);
            Record(sink, ApplicationState.Loading);
            Record(sink, ApplicationState.Refuge);

            Assert.That(sink.Count, Is.EqualTo(1));
            Assert.That(sink.TotalRecorded, Is.EqualTo(2));
            Assert.That(sink.Snapshot()[0].CurrentState, Is.EqualTo(ApplicationState.Refuge));
        }

        [Test]
        public void SnapshotsCannotMutateTheBufferAndStayStableAfterNewRecords()
        {
            var sink = new BoundedDiagnosticsSink(2);
            Record(sink, ApplicationState.Loading);
            Record(sink, ApplicationState.Refuge);
            var firstSnapshot = sink.Snapshot();
            var secondSnapshot = sink.Snapshot();

            firstSnapshot[0] = Event(ApplicationState.Shutdown);
            Assert.That(sink.Snapshot()[0].CurrentState, Is.EqualTo(ApplicationState.Loading));
            Assert.That(secondSnapshot[0].CurrentState, Is.EqualTo(ApplicationState.Loading));
            Record(sink, ApplicationState.Failed);
            Assert.That(secondSnapshot[1].CurrentState, Is.EqualTo(ApplicationState.Refuge));
            Assert.That(sink.Snapshot()[1].CurrentState, Is.EqualTo(ApplicationState.Failed));
        }

        [TestCase(-1, 0, 0)]
        [TestCase(99, 0, 0)]
        [TestCase(0, -1, 0)]
        [TestCase(0, 99, 0)]
        [TestCase(0, 0, -1)]
        [TestCase(0, 0, 99)]
        public void DiagnosticEventRejectsUndefinedEnums(int code, int previous, int current)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiagnosticEvent(
                (DiagnosticCode)code, (ApplicationState)previous, (ApplicationState)current));
        }

        private static DiagnosticEvent Event(ApplicationState current)
        {
            return new DiagnosticEvent(DiagnosticCode.StateChanged,
                ApplicationState.Bootstrap, current);
        }

        private static void Record(BoundedDiagnosticsSink sink, ApplicationState current)
        {
            var diagnosticEvent = Event(current);
            sink.Record(in diagnosticEvent);
        }
    }
}
