using System;
using NUnit.Framework;
using Praxen.Game.Application.Input;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class GestureValueTests
    {
        [TestCase(double.NaN, 0)]
        [TestCase(double.PositiveInfinity, 0)]
        [TestCase(0, double.NegativeInfinity)]
        public void NonFiniteCoordinatesAreRejected(double x, double y)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NormalizedPoint(x, y));
        }

        [TestCase(0, 100)]
        [TestCase(100, 0)]
        [TestCase(-1, 100)]
        public void ScreenDimensionsMustBePositive(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ScreenMetrics(width, height));
        }

        [TestCase(0, SamplePhase.Began, 1)]
        [TestCase(-1, SamplePhase.Began, 1)]
        [TestCase(1, (SamplePhase)99, 1)]
        [TestCase(1, SamplePhase.Began, -1)]
        public void InvalidTouchIdentifiersPhasesOrTimestampsAreRejected(
            long pointer, SamplePhase phase, long time)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TouchSample(pointer, phase, new NormalizedPoint(0, 0), time));
        }

        [TestCase((PointerOwnerKind)99, 0)]
        [TestCase(PointerOwnerKind.Ui, 0)]
        [TestCase(PointerOwnerKind.Gameplay, 42)]
        [TestCase(PointerOwnerKind.Excluded, 42)]
        public void OwnershipMustHaveAValidKindAndMatchingControlIdentity(
            PointerOwnerKind kind, int controlId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PointerOwnership(kind, checked((ulong)controlId)));
        }

        [Test]
        public void UiControlIdentityPreservesEntityIdsLargerThanThirtyTwoBits()
        {
            var owner = new PointerOwnership(PointerOwnerKind.Ui, 4294967338UL);

            Assert.That(owner.Kind, Is.EqualTo(PointerOwnerKind.Ui));
            Assert.That(owner.ControlId, Is.EqualTo(4294967338UL));
        }

        [TestCase(-1, InteractionPhaseKind.Inactive)]
        [TestCase(0, InteractionPhaseKind.EnemySequence)]
        [TestCase(0, InteractionPhaseKind.PlayerOpening)]
        [TestCase(1, (InteractionPhaseKind)99)]
        public void PhaseIdentityMustBeNonnegativeAndActivePhasesMustBePositive(
            long id, InteractionPhaseKind kind)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new InteractionPhase(id, kind));
        }

        [TestCase(0, 1)]
        [TestCase(-0.01, 1)]
        [TestCase(double.NaN, 1)]
        [TestCase(double.PositiveInfinity, 1)]
        [TestCase(0.06, 0)]
        [TestCase(0.06, -1)]
        public void TuningRequiresFinitePositiveTravelAndPositiveDuration(double threshold, long time)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GestureTuning(threshold, time));
        }

        [Test]
        public void PhaseContextStartsInactiveAndCanAdvanceThroughInactiveIdentities()
        {
            var context = new GesturePhaseContext();
            Assert.That(context.Current.Id, Is.Zero);
            Assert.That(context.Current.Kind, Is.EqualTo(InteractionPhaseKind.Inactive));
            context.SetPhase(new InteractionPhase(1, InteractionPhaseKind.EnemySequence));
            context.SetPhase(new InteractionPhase(2, InteractionPhaseKind.Inactive));
            context.SetPhase(new InteractionPhase(3, InteractionPhaseKind.PlayerOpening));

            IInteractionPhaseSource port = context;
            Assert.That(port.Current.Id, Is.EqualTo(3));
            Assert.That(port.Current.Kind, Is.EqualTo(InteractionPhaseKind.PlayerOpening));
        }

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(5)]
        public void PhaseContextRejectsDuplicateAndRegressingIdsWithoutChangingCurrent(long id)
        {
            var context = new GesturePhaseContext();
            context.SetPhase(new InteractionPhase(5, InteractionPhaseKind.EnemySequence));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                context.SetPhase(new InteractionPhase(id, InteractionPhaseKind.Inactive)));
            Assert.That(context.Current.Id, Is.EqualTo(5));
            Assert.That(context.Current.Kind, Is.EqualTo(InteractionPhaseKind.EnemySequence));
        }

        [Test]
        public void RecognizerRejectsDefaultTouchAndScreenValues()
        {
            var recognizer = new CardinalGestureRecognizer();
            var phase = new InteractionPhase(1, InteractionPhaseKind.EnemySequence);
            var sample = new TouchSample(1, SamplePhase.Began, new NormalizedPoint(0, 0), 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => recognizer.Process(
                default(TouchSample), PointerOwnership.Gameplay, phase, new ScreenMetrics(100, 100)));
            Assert.Throws<ArgumentOutOfRangeException>(() => recognizer.Process(
                sample, PointerOwnership.Gameplay, phase, default(ScreenMetrics)));
        }

        [TestCase(0, 0, 1, SwipeDirection.Right, GestureIntent.Parry,
            InteractionPhaseKind.EnemySequence)]
        [TestCase(1, -1, 1, SwipeDirection.Right, GestureIntent.Parry,
            InteractionPhaseKind.EnemySequence)]
        [TestCase(1, 0, 0, SwipeDirection.Right, GestureIntent.Parry,
            InteractionPhaseKind.EnemySequence)]
        [TestCase(1, 0, 1, (SwipeDirection)99, GestureIntent.Parry,
            InteractionPhaseKind.EnemySequence)]
        [TestCase(1, 0, 1, SwipeDirection.Right, (GestureIntent)99,
            InteractionPhaseKind.EnemySequence)]
        [TestCase(1, 0, 1, SwipeDirection.Right, GestureIntent.Attack,
            InteractionPhaseKind.EnemySequence)]
        [TestCase(1, 0, 1, SwipeDirection.Right, GestureIntent.Parry,
            InteractionPhaseKind.Inactive)]
        public void CommandRejectsInvalidIdentityTimeEnumsOrPhaseIntent(
            long sequence, long time, long pointer, SwipeDirection direction,
            GestureIntent intent, InteractionPhaseKind kind)
        {
            var phase = new InteractionPhase(1, kind);
            Assert.Throws<ArgumentOutOfRangeException>(() => new GestureCommand(sequence, time,
                pointer, direction, intent, phase, new NormalizedPoint(0, 0),
                new NormalizedPoint(0.1, 0)));
        }
    }
}
