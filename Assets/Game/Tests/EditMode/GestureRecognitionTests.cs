using NUnit.Framework;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class GestureRecognitionTests
    {
        private static readonly ScreenMetrics Square = new ScreenMetrics(1000, 1000);
        private static readonly InteractionPhase Enemy =
            new InteractionPhase(7, InteractionPhaseKind.EnemySequence);

        [TestCase(0.059999, false)]
        [TestCase(0.06, false)]
        [TestCase(0.060001, true)]
        public void TravelMustStrictlyExceedSixPercent(double x, bool expected)
        {
            var recognizer = new CardinalGestureRecognizer();
            Process(recognizer, SamplePhase.Began, 0, 0, 100000);

            var command = Process(recognizer, SamplePhase.Moved, x, 0, 100001);

            Assert.That(command.HasValue, Is.EqualTo(expected));
        }

        [Test]
        public void DiagonalTravelUsesDistanceInsteadOfOnlyItsLargestAxis()
        {
            var recognizer = new CardinalGestureRecognizer();
            Process(recognizer, SamplePhase.Began, 0, 0, 100000);

            var command = Process(recognizer, SamplePhase.Moved, 0.05, 0.04, 100001);

            Assert.That(command.HasValue, Is.True);
            Assert.That(command.Value.Direction, Is.EqualTo(SwipeDirection.Right));
        }

        [TestCase(350000, true)]
        [TestCase(350001, false)]
        public void DeadlineIncludesItsLastMicrosecond(long elapsedUs, bool expected)
        {
            var recognizer = new CardinalGestureRecognizer();
            Process(recognizer, SamplePhase.Began, 0, 0, 100000);

            var command = Process(recognizer, SamplePhase.Moved, 0.1, 0, 100000 + elapsedUs);

            Assert.That(command.HasValue, Is.EqualTo(expected));
        }

        [TestCase(0.1, 0, SwipeDirection.Right)]
        [TestCase(-0.1, 0, SwipeDirection.Left)]
        [TestCase(0, 0.1, SwipeDirection.Up)]
        [TestCase(0, -0.1, SwipeDirection.Down)]
        [TestCase(0.1, 0.1, SwipeDirection.Right)]
        [TestCase(-0.1, -0.1, SwipeDirection.Left)]
        [TestCase(0.1, 0.1000000000005, SwipeDirection.Right)]
        [TestCase(0.1, 0.100000000002, SwipeDirection.Up)]
        public void DirectionUsesCardinalDominanceAndHorizontalFloatingPointTies(
            double x, double y, SwipeDirection direction)
        {
            var recognizer = new CardinalGestureRecognizer();
            Process(recognizer, SamplePhase.Began, 0, 0, 0);

            var command = Process(recognizer, SamplePhase.Moved, x, y, 1);

            Assert.That(command.HasValue, Is.True);
            Assert.That(command.Value.Direction, Is.EqualTo(direction));
        }

        [TestCase(2000, 1000, 0.04, 0, SwipeDirection.Right)]
        [TestCase(1000, 2000, 0, 0.04, SwipeDirection.Up)]
        [TestCase(2000, 1000, 0.05, 0.08, SwipeDirection.Right)]
        [TestCase(1000, 2000, 0.08, 0.05, SwipeDirection.Up)]
        [TestCase(2000, 1000, 0.05, 0.1000000000005, SwipeDirection.Right)]
        [TestCase(2000, 1000, 0.05, 0.100000000002, SwipeDirection.Up)]
        public void RectangularScreensMeasureTravelAndDirectionInPhysicalProportions(
            int width, int height, double x, double y, SwipeDirection direction)
        {
            var recognizer = new CardinalGestureRecognizer();
            var metrics = new ScreenMetrics(width, height);
            Process(recognizer, SamplePhase.Began, 0, 0, 0, metrics);

            var command = Process(recognizer, SamplePhase.Moved, x, y, 1, metrics);

            Assert.That(command.HasValue, Is.True);
            Assert.That(command.Value.Direction, Is.EqualTo(direction));
        }

        [Test]
        public void FirstCrossingPreservesSourceTimestampCapturedPhaseAndEndpoints()
        {
            var recognizer = new CardinalGestureRecognizer();
            Process(recognizer, SamplePhase.Began, 0.2, 0.3, 100000);
            Assert.That(Process(recognizer, SamplePhase.Moved, 0.22, 0.3, 140123), Is.Null);

            var result = Process(recognizer, SamplePhase.Moved, 0.27, 0.31, 183456);

            Assert.That(result.HasValue, Is.True);
            var command = result.Value;
            Assert.That(command.Sequence, Is.EqualTo(1));
            Assert.That(command.InputTimestampUs, Is.EqualTo(183456));
            Assert.That(command.PointerId, Is.EqualTo(1));
            Assert.That(command.Intent, Is.EqualTo(GestureIntent.Parry));
            Assert.That(command.Phase.Id, Is.EqualTo(7));
            Assert.That(command.Phase.Kind, Is.EqualTo(InteractionPhaseKind.EnemySequence));
            Assert.That(command.Start.X, Is.EqualTo(0.2));
            Assert.That(command.Start.Y, Is.EqualTo(0.3));
            Assert.That(command.End.X, Is.EqualTo(0.27));
            Assert.That(command.End.Y, Is.EqualTo(0.31));
        }

        [Test]
        public void EndRecognizesWhenItIsTheFirstDeliveredCrossing()
        {
            var recognizer = new CardinalGestureRecognizer();
            Process(recognizer, SamplePhase.Began, 0, 0, 100);

            var result = Process(recognizer, SamplePhase.Ended, 0, -0.1, 35100);

            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value.InputTimestampUs, Is.EqualTo(35100));
            Assert.That(result.Value.Direction, Is.EqualTo(SwipeDirection.Down));
        }

        [Test]
        public void CancelNeverRecognizesEvenWhenItCrossesTheThreshold()
        {
            var recognizer = new CardinalGestureRecognizer();
            Process(recognizer, SamplePhase.Began, 0, 0, 100);

            Assert.That(Process(recognizer, SamplePhase.Cancelled, 0.2, 0, 200), Is.Null);
            Assert.That(Process(recognizer, SamplePhase.Moved, 0.3, 0, 300), Is.Null);
        }

        [Test]
        public void OpeningPhaseProducesAnAttack()
        {
            var recognizer = new CardinalGestureRecognizer();
            var phase = new InteractionPhase(8, InteractionPhaseKind.PlayerOpening);
            var begin = new TouchSample(1, SamplePhase.Began, new NormalizedPoint(0, 0), 0);
            var move = new TouchSample(1, SamplePhase.Moved, new NormalizedPoint(0.1, 0), 1);
            recognizer.Process(begin, PointerOwnership.Gameplay, phase, Square);

            var command = recognizer.Process(move, PointerOwnership.Gameplay, phase, Square);

            Assert.That(command.HasValue, Is.True);
            Assert.That(command.Value.Intent, Is.EqualTo(GestureIntent.Attack));
            Assert.That(command.Value.Phase.Id, Is.EqualTo(8));
        }

        [Test]
        public void CustomTuningControlsTravelAndTimeTogether()
        {
            var recognizer = new CardinalGestureRecognizer(new GestureTuning(0.1, 100));
            Process(recognizer, SamplePhase.Began, 0, 0, 0);
            Assert.That(Process(recognizer, SamplePhase.Moved, 0.09, 0, 99), Is.Null);

            Assert.That(Process(recognizer, SamplePhase.Moved, 0.11, 0, 100).HasValue, Is.True);
        }

        [Test]
        public void LateEndCannotRecognizeItsFirstCrossing()
        {
            var recognizer = new CardinalGestureRecognizer();
            Process(recognizer, SamplePhase.Began, 0, 0, 0);

            Assert.That(Process(recognizer, SamplePhase.Ended, 0.2, 0, 350001), Is.Null);
            Process(recognizer, SamplePhase.Began, 0, 0, 350002);
            Assert.That(Process(recognizer, SamplePhase.Moved, 0.2, 0, 350003).Value.Sequence,
                Is.EqualTo(1));
        }

        [Test]
        public void CrossingOutsideNormalizedScreenBoundsStillRecognizes()
        {
            var recognizer = new CardinalGestureRecognizer();
            Process(recognizer, SamplePhase.Began, 0.01, 0.5, 0);

            var result = Process(recognizer, SamplePhase.Moved, -0.1, 0.5, 1);

            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value.Direction, Is.EqualTo(SwipeDirection.Left));
            Assert.That(result.Value.End.X, Is.EqualTo(-0.1));
        }

        private static GestureCommand? Process(CardinalGestureRecognizer recognizer,
            SamplePhase phase, double x, double y, long time, ScreenMetrics? metrics = null)
        {
            var sample = new TouchSample(1, phase, new NormalizedPoint(x, y), time);
            return recognizer.Process(sample, PointerOwnership.Gameplay, Enemy, metrics ?? Square);
        }
    }
}
