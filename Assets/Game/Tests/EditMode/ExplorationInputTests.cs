using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Application.Input;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class ExplorationInputTests
    {
        private static readonly InteractionPhase Exploration = new InteractionPhase(9, InteractionPhaseKind.Exploration);
        private static readonly ScreenMetrics Portrait = new ScreenMetrics(1000, 2000);
        private ExplorationInputController _controller;
        private List<NormalizedPoint> _taps;
        private List<NormalizedPoint> _drags;

        [SetUp]
        public void SetUp()
        {
            _controller = new ExplorationInputController(9);
            _taps = new List<NormalizedPoint>();
            _drags = new List<NormalizedPoint>();
            _controller.DestinationTapped += _taps.Add;
            _controller.InspectionDragged += (x, y) => _drags.Add(new NormalizedPoint(x, y));
        }

        [Test]
        public void TapEmitsEndPositionAndThresholdIsInclusive()
        {
            Send(1, SamplePhase.Began, 0, 0, 10);
            Send(1, SamplePhase.Ended, .025, 0, 20);
            Assert.That(_taps.Count, Is.EqualTo(1));
            Assert.That(_taps[0].X, Is.EqualTo(.025));
            Assert.That(_drags, Is.Empty);
        }

        [Test]
        public void PortraitVerticalTravelUsesShorterScreenDimension()
        {
            Send(1, SamplePhase.Began, .5, .5, 10);
            Send(1, SamplePhase.Moved, .5, .52, 20);
            Send(1, SamplePhase.Ended, .5, .53, 30);
            Assert.That(_taps, Is.Empty);
            Assert.That(_drags.Count, Is.EqualTo(2));
            Assert.That(_drags[0].Y, Is.EqualTo(.04).Within(1e-12));
            Assert.That(_drags[1].Y, Is.EqualTo(.02).Within(1e-12));
        }

        [Test]
        public void DragReturningToStartNeverBecomesTap()
        {
            Send(1, SamplePhase.Began, .5, .5, 10);
            Send(1, SamplePhase.Moved, .6, .5, 20);
            Send(1, SamplePhase.Ended, .5, .5, 30);
            Assert.That(_taps, Is.Empty);
            Assert.That(_drags.Count, Is.EqualTo(2));
        }

        [Test]
        public void EndWithoutMoveRecognizesDrag()
        {
            Send(1, SamplePhase.Began, .5, .5, 10);
            Send(1, SamplePhase.Ended, .6, .5, 20);
            Assert.That(_taps, Is.Empty);
            Assert.That(_drags.Count, Is.EqualTo(1));
            Assert.That(_drags[0].X, Is.EqualTo(.1).Within(1e-12));
        }

        [Test]
        public void UiStartCannotAcquireGameplayOwnershipLater()
        {
            Send(1, SamplePhase.Began, .5, .5, 10, new PointerOwnership(PointerOwnerKind.Ui, 1));
            Send(1, SamplePhase.Moved, .7, .5, 20);
            Send(1, SamplePhase.Ended, .7, .5, 30);
            AssertNoOutput();
        }

        [Test]
        public void SecondContactRemainsExcludedAfterFirstReleases()
        {
            Send(1, SamplePhase.Began, .5, .5, 10);
            Send(2, SamplePhase.Began, .5, .5, 11);
            Send(1, SamplePhase.Cancelled, .5, .5, 12);
            Send(2, SamplePhase.Ended, .5, .5, 13);
            AssertNoOutput();
            Send(2, SamplePhase.Began, .5, .5, 14);
            Send(2, SamplePhase.Ended, .5, .5, 15);
            Assert.That(_taps.Count, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateBeginCancelsContactWithoutReacquiringIt()
        {
            Send(1, SamplePhase.Began, .5, .5, 10);
            Send(1, SamplePhase.Began, .5, .5, 11);
            Send(1, SamplePhase.Ended, .5, .5, 12);
            AssertNoOutput();
        }

        [Test]
        public void CancellationDoesNotEmitMovementOrTap()
        {
            Send(1, SamplePhase.Began, .5, .5, 10);
            Send(1, SamplePhase.Cancelled, .8, .8, 20);
            AssertNoOutput();
        }

        [Test]
        public void TimestampInversionPermanentlyInvalidatesContact()
        {
            Send(1, SamplePhase.Began, .5, .5, 20);
            Send(1, SamplePhase.Moved, .5, .5, 10);
            Send(1, SamplePhase.Ended, .5, .5, 30);
            AssertNoOutput();
        }

        [Test]
        public void MetricMismatchInvalidatesContactEvenIfMetricsReturn()
        {
            Send(1, SamplePhase.Began, .5, .5, 10);
            Send(1, SamplePhase.Moved, .5, .5, 20, metrics: new ScreenMetrics(2000, 1000));
            Send(1, SamplePhase.Ended, .5, .5, 30);
            AssertNoOutput();
        }

        [Test]
        public void PhaseMismatchInvalidatesContactEvenIfPhaseReturns()
        {
            Send(1, SamplePhase.Began, .5, .5, 10);
            Send(1, SamplePhase.Moved, .5, .5, 20, phase: new InteractionPhase(10, InteractionPhaseKind.Exploration));
            Send(1, SamplePhase.Ended, .5, .5, 30);
            AssertNoOutput();
        }

        [Test]
        public void ResetPreventsOldEndAndAllowsFreshContact()
        {
            Send(1, SamplePhase.Began, .5, .5, 10);
            _controller.Reset(10);
            var next = new InteractionPhase(10, InteractionPhaseKind.Exploration);
            Send(1, SamplePhase.Ended, .5, .5, 20, phase: next);
            AssertNoOutput();
            Send(2, SamplePhase.Began, .5, .5, 30, phase: next);
            Send(2, SamplePhase.Ended, .5, .5, 40, phase: next);
            Assert.That(_taps.Count, Is.EqualTo(1));
        }

        [Test]
        public void NonExplorationPhaseAndStalePhaseCannotTap()
        {
            var combat = new InteractionPhase(9, InteractionPhaseKind.PlayerOpening);
            Send(1, SamplePhase.Began, .5, .5, 10, phase: combat);
            Send(1, SamplePhase.Ended, .5, .5, 20, phase: combat);
            var stale = new InteractionPhase(8, InteractionPhaseKind.Exploration);
            Send(2, SamplePhase.Began, .5, .5, 30, phase: stale);
            Send(2, SamplePhase.Ended, .5, .5, 40, phase: stale);
            AssertNoOutput();
        }

        [Test]
        public void ContactOverflowCancelsHeldOwnerAndFreshBeginCanRecover()
        {
            Send(1, SamplePhase.Began, .5, .5, 1);
            for (var id = 2; id <= 33; id++)
                Send(id, SamplePhase.Began, .5, .5, id);
            Send(1, SamplePhase.Ended, .5, .5, 40);
            Send(33, SamplePhase.Ended, .5, .5, 41);
            AssertNoOutput();
            _controller.Reset(9);
            Send(40, SamplePhase.Began, .5, .5, 50);
            Send(40, SamplePhase.Ended, .5, .5, 51);
            Assert.That(_taps.Count, Is.EqualTo(1));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void OverflowQuarantinesDuplicateHeldGameplayExcludedAndUiContactsUntilReset(long heldPointer)
        {
            Send(1, SamplePhase.Began, .5, .5, 1);
            Send(2, SamplePhase.Began, .5, .5, 2);
            Send(3, SamplePhase.Began, .5, .5, 3, new PointerOwnership(PointerOwnerKind.Ui, 1));
            for (var id = 4; id <= 33; id++)
                Send(id, SamplePhase.Began, .5, .5, id);
            Send(heldPointer, SamplePhase.Began, .5, .5, 34);
            Send(heldPointer, SamplePhase.Ended, .5, .5, 35);
            AssertNoOutput();
            Send(40, SamplePhase.Began, .5, .5, 36);
            Send(40, SamplePhase.Moved, .7, .5, 37);
            Send(40, SamplePhase.Ended, .7, .5, 38);
            AssertNoOutput();
            _controller.Reset(9);
            Send(40, SamplePhase.Began, .5, .5, 39);
            Send(40, SamplePhase.Ended, .5, .5, 40);
            Assert.That(_taps.Count, Is.EqualTo(1));
        }

        private void AssertNoOutput()
        {
            Assert.That(_taps, Is.Empty);
            Assert.That(_drags, Is.Empty);
        }

        private void Send(long id, SamplePhase samplePhase, double x, double y, long time,
            PointerOwnership? owner = null, ScreenMetrics? metrics = null, InteractionPhase? phase = null)
        {
            _controller.Process(new OrderedTouchRecord(new TouchSample(id, samplePhase,
                new NormalizedPoint(x, y), time), owner ?? PointerOwnership.Gameplay,
                metrics ?? Portrait), phase ?? Exploration);
        }
    }
}
