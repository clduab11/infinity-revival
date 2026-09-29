using NUnit.Framework;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class GestureContactTests
    {
        private static readonly ScreenMetrics Screen = new ScreenMetrics(1000, 1000);
        private static readonly InteractionPhase Enemy =
            new InteractionPhase(1, InteractionPhaseKind.EnemySequence);

        [Test]
        public void GameplayOwnershipSurvivesCrossingIntoUi()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 10);

            var command = Send(recognizer, 1, SamplePhase.Moved, 0.2, 11,
                new PointerOwnership(PointerOwnerKind.Ui, 42));

            Assert.That(command.HasValue, Is.True);
        }

        [Test]
        public void UiBeginNeverTurnsIntoGameplayAfterLeavingItsControl()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 10,
                new PointerOwnership(PointerOwnerKind.Ui, 42));

            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 11), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Ended, 0.3, 12), Is.Null);
            Send(recognizer, 1, SamplePhase.Began, 0, 13);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 14).HasValue, Is.True);
        }

        [Test]
        public void UiContactDoesNotOccupyTheGameplayPointerSlot()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 10,
                new PointerOwnership(PointerOwnerKind.Ui, 42));
            Send(recognizer, 2, SamplePhase.Began, 0, 11);

            Assert.That(Send(recognizer, 2, SamplePhase.Moved, 0.2, 12).HasValue, Is.True);
        }

        [TestCase(SamplePhase.Ended)]
        [TestCase(SamplePhase.Cancelled)]
        public void SecondaryHeldPointerCannotInheritRecognitionAfterPrimaryRelease(
            SamplePhase terminal)
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0);
            Send(recognizer, 2, SamplePhase.Began, 0, 1);
            Send(recognizer, 1, terminal, 0, 2);

            Assert.That(Send(recognizer, 2, SamplePhase.Moved, 0.2, 3), Is.Null);
            Assert.That(Send(recognizer, 2, SamplePhase.Began, 0, 4), Is.Null);
            Assert.That(Send(recognizer, 2, SamplePhase.Moved, 0.2, 5), Is.Null);
            Send(recognizer, 2, terminal, 0.2, 6);
            Send(recognizer, 2, SamplePhase.Began, 0, 7);
            Assert.That(Send(recognizer, 2, SamplePhase.Moved, 0.2, 8).HasValue, Is.True);
        }

        [Test]
        public void CommittedPointerKeepsTheSlotUntilRelease()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 1).HasValue, Is.True);
            Send(recognizer, 2, SamplePhase.Began, 0, 2);

            Assert.That(Send(recognizer, 2, SamplePhase.Moved, 0.2, 3), Is.Null);
        }

        [Test]
        public void DuplicateBeginCannotReplaceTheStartOrCapturedOwnership()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0);
            Send(recognizer, 1, SamplePhase.Began, 0.5, 1,
                new PointerOwnership(PointerOwnerKind.Ui, 42));

            var result = Send(recognizer, 1, SamplePhase.Moved, 0.1, 2);

            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value.Start.X, Is.EqualTo(0));
        }

        [Test]
        public void DuplicateAndTerminalSamplesNeverEmitASecondCommand()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 1).HasValue, Is.True);

            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.3, 2), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Began, 0, 3), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Ended, 0.4, 4), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Ended, 0.4, 4), Is.Null);
        }

        [TestCase(2, InteractionPhaseKind.EnemySequence)]
        [TestCase(2, InteractionPhaseKind.PlayerOpening)]
        [TestCase(1, InteractionPhaseKind.PlayerOpening)]
        [TestCase(2, InteractionPhaseKind.Inactive)]
        public void PhaseIdentityOrKindChangeCancelsWithoutReinterpretingIntent(
            long id, InteractionPhaseKind kind)
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0);
            var changed = new InteractionPhase(id, kind);

            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 1, phase: changed), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Began, 0, 2), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.3, 3), Is.Null);
        }

        [Test]
        public void PhaseChangeObservedThroughAnotherPointerStillCancelsPrimary()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0);
            Send(recognizer, 2, SamplePhase.Began, 0, 1);
            Send(recognizer, 2, SamplePhase.Moved, 0.01, 2,
                phase: new InteractionPhase(2, InteractionPhaseKind.PlayerOpening));

            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 3), Is.Null);
        }

        [Test]
        public void InactiveBeginCannotArmWhenAnActivePhaseArrives()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0, phase: InteractionPhase.Inactive);

            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 1), Is.Null);
            Send(recognizer, 1, SamplePhase.Ended, 0.2, 2);
            Send(recognizer, 1, SamplePhase.Began, 0, 3);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 4).HasValue, Is.True);
        }

        [Test]
        public void InactiveHeldContactDoesNotExcludeAFreshActiveGameplayContact()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0, phase: InteractionPhase.Inactive);
            Send(recognizer, 2, SamplePhase.Began, 0, 1);

            var command = Send(recognizer, 2, SamplePhase.Moved, 0.2, 2);

            Assert.That(command.HasValue, Is.True);
            Assert.That(command.Value.PointerId, Is.EqualTo(2));
            Assert.That(command.Value.Intent, Is.EqualTo(GestureIntent.Parry));
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 3), Is.Null);
            Send(recognizer, 2, SamplePhase.Ended, 0.2, 4);
            Assert.That(Send(recognizer, 1, SamplePhase.Began, 0, 5), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.3, 6), Is.Null);
            Send(recognizer, 1, SamplePhase.Ended, 0.3, 7);
            Send(recognizer, 1, SamplePhase.Began, 0, 8);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 9).Value.Sequence,
                Is.EqualTo(2));
        }

        [Test]
        public void DeadlineExpiryCannotBeRearmedByLaterMovementOrBegin()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.01, 350001), Is.Null);

            Assert.That(Send(recognizer, 1, SamplePhase.Began, 0, 350002), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 350003), Is.Null);
        }

        [Test]
        public void TimestampRegressionCancelsUntilRelease()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 100);
            Send(recognizer, 1, SamplePhase.Moved, 0.01, 200);

            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 199), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.3, 201), Is.Null);
            Send(recognizer, 1, SamplePhase.Ended, 0.3, 202);
            Send(recognizer, 1, SamplePhase.Began, 0, 203);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 204).HasValue, Is.True);
        }

        [Test]
        public void RegressingDuplicateBeginCancelsInsteadOfRestartingTheClock()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 100);
            Send(recognizer, 1, SamplePhase.Moved, 0.01, 200);

            Assert.That(Send(recognizer, 1, SamplePhase.Began, 0, 199), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 201), Is.Null);
        }

        [Test]
        public void SameTimestampSamplesRemainValid()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 100);

            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 100).HasValue, Is.True);
        }

        [Test]
        public void ScreenChangeCancelsTheContactInsteadOfRescalingItsOrigin()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0);
            var changed = new ScreenMetrics(2000, 1000);

            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.1, 1, metrics: changed), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 2), Is.Null);
        }

        [Test]
        public void ScreenChangeObservedThroughAnExcludedPointerCancelsPrimary()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0);
            Send(recognizer, 2, SamplePhase.Began, 0, 1);
            Send(recognizer, 2, SamplePhase.Moved, 0.01, 2,
                metrics: new ScreenMetrics(2000, 1000));

            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 3), Is.Null);
        }

        [Test]
        public void CancelAllBlocksEveryLiveContactUntilItsTerminalSample()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0);
            Send(recognizer, 2, SamplePhase.Began, 0, 1);
            recognizer.CancelAll();

            Assert.That(Send(recognizer, 1, SamplePhase.Began, 0, 2), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 3), Is.Null);
            Send(recognizer, 1, SamplePhase.Ended, 0.2, 4);
            Assert.That(Send(recognizer, 2, SamplePhase.Moved, 0.2, 5), Is.Null);
            Send(recognizer, 2, SamplePhase.Cancelled, 0.2, 6);
            Send(recognizer, 2, SamplePhase.Began, 0, 7);
            Assert.That(Send(recognizer, 2, SamplePhase.Moved, 0.2, 8).HasValue, Is.True);
        }

        [Test]
        public void ResetContactsAllowsFreshBeginWhilePreservingCommandSequence()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 1).Value.Sequence, Is.EqualTo(1));
            recognizer.ResetContacts();
            Send(recognizer, 1, SamplePhase.Began, 0, 2);

            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 3).Value.Sequence, Is.EqualTo(2));
        }

        [Test]
        public void PreexistingExcludedContactCannotArmWithoutReleasing()
        {
            var recognizer = new CardinalGestureRecognizer();
            Send(recognizer, 1, SamplePhase.Began, 0, 0, PointerOwnership.Excluded);
            Assert.That(Send(recognizer, 1, SamplePhase.Began, 0, 1), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 2), Is.Null);
            Send(recognizer, 1, SamplePhase.Ended, 0.2, 3);
            Send(recognizer, 1, SamplePhase.Began, 0, 4);

            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 5).HasValue, Is.True);
        }

        [Test]
        public void UnmatchedMoveOrEndCannotInventAContact()
        {
            var recognizer = new CardinalGestureRecognizer();

            Assert.That(Send(recognizer, 1, SamplePhase.Moved, 0.2, 1), Is.Null);
            Assert.That(Send(recognizer, 1, SamplePhase.Ended, 0.3, 2), Is.Null);
        }

        private static GestureCommand? Send(CardinalGestureRecognizer recognizer, long pointer,
            SamplePhase samplePhase, double x, long time, PointerOwnership? ownership = null,
            InteractionPhase? phase = null, ScreenMetrics? metrics = null)
        {
            var sample = new TouchSample(pointer, samplePhase, new NormalizedPoint(x, 0), time);
            return recognizer.Process(sample, ownership ?? PointerOwnership.Gameplay,
                phase ?? Enemy, metrics ?? Screen);
        }
    }
}
