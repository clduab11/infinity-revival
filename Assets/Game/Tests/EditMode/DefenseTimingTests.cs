using System;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using static Praxen.Game.Tests.EditMode.DefenseRulesTests;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class DefenseTimingTests
    {
        [TestCase(49999, DefenseOutcome.Hit)]
        [TestCase(50000, DefenseOutcome.Dodged)]
        [TestCase(230000, DefenseOutcome.Dodged)]
        [TestCase(230001, DefenseOutcome.Hit)]
        [TestCase(360000, DefenseOutcome.Hit)]
        public void DodgeAvoidanceHasInclusiveAuthoredBoundaries(long impactUs, DefenseOutcome expected)
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 0);

            Assert.That(player.ResolveImpact(Strike(), impactUs).Outcome, Is.EqualTo(expected));
        }

        [TestCase(230000, PlayerCombatState.Dodging)]
        [TestCase(230001, PlayerCombatState.Recovery)]
        [TestCase(359999, PlayerCombatState.Recovery)]
        [TestCase(360000, PlayerCombatState.Ready)]
        public void DodgeTailUsesItsOriginalEndWithoutExtraRecovery(long timeUs, PlayerCombatState expected)
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.DodgeRight), 0);

            player.AdvanceTo(timeUs);

            Assert.That(player.State, Is.EqualTo(expected));
            Assert.That(player.DodgeCharges, Is.EqualTo(2));
        }

        [TestCase(59999, false)]
        [TestCase(60000, true)]
        [TestCase(199999, true)]
        [TestCase(200000, true)]
        [TestCase(200001, false)]
        public void ParryWindowIsInclusiveAndRejectsEarlyBuffering(long commandTimeUs, bool accepted)
        {
            var player = new DefenseCombatant();

            Assert.That(player.TryParry(Strike(), 200000, SwipeDirection.Right, commandTimeUs), Is.EqualTo(accepted));

            Assert.That(player.State, Is.EqualTo(accepted ? PlayerCombatState.Parrying : PlayerCombatState.Ready));
        }

        [Test]
        public void EarlyParryRejectionAllowsAValidParryAtTheWindowOpening()
        {
            var player = new DefenseCombatant();
            var strike = Strike();

            Assert.That(player.TryParry(strike, 200000, SwipeDirection.Right, 59999), Is.False);
            Assert.That(player.TryParry(strike, 200000, SwipeDirection.Right, 60000), Is.True);
            Assert.That(player.ResolveImpact(strike, 200000).Outcome, Is.EqualTo(DefenseOutcome.Parried));
        }

        [Test]
        public void AdvancingToExactImpactKeepsParryArmedForResolution()
        {
            var player = new DefenseCombatant();
            var strike = Strike();
            player.TryParry(strike, 200000, SwipeDirection.Right, 60000);

            player.AdvanceTo(200000);

            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Parrying));
            Assert.That(player.ResolveImpact(strike, 200000).Outcome, Is.EqualTo(DefenseOutcome.Parried));
            Assert.That(player.ActionEndsUs, Is.EqualTo(450000));
        }

        [Test]
        public void UnresolvedParryExpiresOnlyAfterImpactAndPreservesCommittedRecovery()
        {
            var player = new DefenseCombatant();
            player.TryParry(Strike(), 200000, SwipeDirection.Right, 60000);

            player.AdvanceTo(200001);

            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Recovery));
            Assert.That(player.ActionEndsUs, Is.EqualTo(450000));
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 449999), Is.False);
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 450000), Is.True);
        }

        [Test]
        public void HitRecoveryRejectsInputUntilExactEnd()
        {
            var player = new DefenseCombatant();
            player.ResolveImpact(Strike(), 100000);

            Assert.That(player.ApplyControl(Control(DefenseCommandKind.GuardPress), 299999), Is.False);
            Assert.That(player.TryParry(Strike(), 300000, SwipeDirection.Right, 299999), Is.False);
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.GuardPress), 300000), Is.True);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Guarding));
        }

        [Test]
        public void DodgeRecoveryRejectsInputUntilExactEnd()
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 0);

            Assert.That(player.ApplyControl(Control(DefenseCommandKind.GuardPress), 359999), Is.False);
            Assert.That(player.TryParry(Strike(), 360000, SwipeDirection.Right, 359999), Is.False);
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.GuardPress), 360000), Is.True);
        }

        [Test]
        public void SuccessfulParryRecoveryRejectsInputUntilExactEnd()
        {
            var player = new DefenseCombatant();
            var strike = Strike();
            player.TryParry(strike, 200000, SwipeDirection.Right, 60000);
            player.ResolveImpact(strike, 200000);

            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 449999), Is.False);
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 450000), Is.True);
        }

        [Test]
        public void StaggerRejectsControlsAndParryUntilExactEnd()
        {
            var player = new DefenseCombatant(new CombatDefenseTuning(maximumGuard: 25));
            player.ApplyControl(Control(DefenseCommandKind.GuardPress), 0);
            player.ResolveImpact(Strike(), 100000);

            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 699999), Is.False);
            Assert.That(player.TryParry(Strike(), 700000, SwipeDirection.Right, 699999), Is.False);
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 700000), Is.True);
        }

        [Test]
        public void LateHitKeepsStaggerDeadlineThenCompletesFullHitRecovery()
        {
            var player = new DefenseCombatant(new CombatDefenseTuning(maximumGuard: 25));
            player.ApplyControl(Control(DefenseCommandKind.GuardPress), 0);
            player.ResolveImpact(Strike(), 100000);

            player.ResolveImpact(Strike(1, id: 2), 699999);

            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Staggered));
            Assert.That(player.ActionEndsUs, Is.EqualTo(700000));
            Assert.That(player.Health, Is.EqualTo(70));
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 700000), Is.False);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Recovery));
            Assert.That(player.ActionEndsUs, Is.EqualTo(899999));
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 899998), Is.False);
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 899999), Is.True);
        }

        [Test]
        public void EarlyHitDoesNotExtendTheOriginalStaggerDeadline()
        {
            var player = new DefenseCombatant(new CombatDefenseTuning(maximumGuard: 25));
            player.ApplyControl(Control(DefenseCommandKind.GuardPress), 0);
            player.ResolveImpact(Strike(), 100000);

            player.ResolveImpact(Strike(1, id: 2), 200000);

            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Staggered));
            Assert.That(player.ActionEndsUs, Is.EqualTo(700000));
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 699999), Is.False);
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 700000), Is.True);
        }

        [TestCase(PlayerCombatState.Dodging, 100, 100, 2, 360000, DodgeSide.Left)]
        [TestCase(PlayerCombatState.Parrying, 100, 100, 3, 100000, DodgeSide.None)]
        [TestCase(PlayerCombatState.Recovery, 70, 100, 3, 200000, DodgeSide.None)]
        [TestCase(PlayerCombatState.Staggered, 100, 0, 3, 600000, DodgeSide.None)]
        [TestCase(PlayerCombatState.Dead, 0, 100, 3, 0, DodgeSide.None)]
        public void GuardReleaseAlwaysSucceedsWithoutChangingCommittedStateOrResources(
            PlayerCombatState state, int health, int guard, int charges, long actionEndUs, DodgeSide side)
        {
            var player = PlayerInState(state);

            Assert.That(player.ApplyControl(Control(DefenseCommandKind.GuardRelease), 0), Is.True);

            Assert.That(player.State, Is.EqualTo(state));
            Assert.That(player.Health, Is.EqualTo(health));
            Assert.That(player.Guard, Is.EqualTo(guard));
            Assert.That(player.DodgeCharges, Is.EqualTo(charges));
            Assert.That(player.ActionEndsUs, Is.EqualTo(actionEndUs));
            Assert.That(player.DodgeSide, Is.EqualTo(side));
            Assert.That(player.GuardHeld, Is.False);
        }

        [Test]
        public void GuardReleaseDoesNotCancelACommittedParry()
        {
            var player = new DefenseCombatant();
            var strike = Strike();
            player.TryParry(strike, 100000, SwipeDirection.Right, 0);

            Assert.That(player.ApplyControl(Control(DefenseCommandKind.GuardRelease), 0), Is.True);

            Assert.That(player.ResolveImpact(strike, 100000).Outcome, Is.EqualTo(DefenseOutcome.Parried));
        }

        [Test]
        public void DodgeChargeRechargesExactlyAfterThreeUndisturbedSeconds()
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 100);

            player.AdvanceTo(3000099);
            Assert.That(player.DodgeCharges, Is.EqualTo(2));
            player.AdvanceTo(3000100);
            Assert.That(player.DodgeCharges, Is.EqualTo(3));
            player.AdvanceTo(100000000);
            Assert.That(player.DodgeCharges, Is.EqualTo(3));
        }

        [Test]
        public void AcceptedDodgeResetsRechargeAndChargesReturnSerially()
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 0);
            player.ApplyControl(Control(DefenseCommandKind.DodgeRight), 360000);
            player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 720000);

            Assert.That(player.DodgeCharges, Is.Zero);
            player.AdvanceTo(3719999);
            Assert.That(player.DodgeCharges, Is.Zero);
            player.AdvanceTo(3720000);
            Assert.That(player.DodgeCharges, Is.EqualTo(1));
            player.AdvanceTo(6720000);
            Assert.That(player.DodgeCharges, Is.EqualTo(2));
            player.AdvanceTo(9720000);
            Assert.That(player.DodgeCharges, Is.EqualTo(3));
        }

        [Test]
        public void ExhaustedOrRecoveryLockedDodgeNeverDelaysRecharge()
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 0);
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeRight), 1), Is.False);
            player.ApplyControl(Control(DefenseCommandKind.DodgeRight), 360000);
            player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 720000);

            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeRight), 3000000), Is.False);
            player.AdvanceTo(3720000);

            Assert.That(player.DodgeCharges, Is.EqualTo(1));
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeRight), 3720000), Is.True);
        }

        [Test]
        public void HitsGuardAndParryDoNotResetDodgeRecharge()
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 0);
            player.ResolveImpact(Strike(1), 1000000);
            player.ApplyControl(Control(DefenseCommandKind.GuardPress), 1200000);
            player.ResolveImpact(Strike(1), 1300000);
            var strike = Strike(7, id: 2);
            player.TryParry(strike, 2000000, SwipeDirection.Right, 1860000);
            player.ResolveImpact(strike, 2000000);

            player.AdvanceTo(3000000);

            Assert.That(player.DodgeCharges, Is.EqualTo(3));
            Assert.That(player.Guard, Is.EqualTo(75));
            Assert.That(player.Health, Is.EqualTo(70));
        }

        [Test]
        public void ReleaseClearsHeldGuardWithoutRestoringSpentResources()
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.GuardPress), 0);
            player.ResolveImpact(Strike(1), 100000);

            player.ReleaseHeldDefense();

            Assert.That(player.GuardHeld, Is.False);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
            Assert.That(player.Guard, Is.EqualTo(75));
            Assert.That(player.Health, Is.EqualTo(100));
        }

        [Test]
        public void ReleaseClearsParryArmingAndPreservesDodgeAndStaggerTimers()
        {
            var parrying = new DefenseCombatant();
            var strike = Strike();
            parrying.TryParry(strike, 200000, SwipeDirection.Right, 60000);
            parrying.ReleaseHeldDefense();
            Assert.That(parrying.ResolveImpact(strike, 200000).Outcome, Is.EqualTo(DefenseOutcome.Hit));

            var dodging = new DefenseCombatant();
            dodging.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 100);
            dodging.ReleaseHeldDefense();
            Assert.That(dodging.DodgeCharges, Is.EqualTo(2));
            Assert.That(dodging.DodgeStartedUs, Is.EqualTo(100));
            Assert.That(dodging.ActionEndsUs, Is.EqualTo(360100));
            Assert.That(dodging.ResolveImpact(strike, 100100).Outcome, Is.EqualTo(DefenseOutcome.Dodged));

            var staggered = new DefenseCombatant(new CombatDefenseTuning(maximumGuard: 20));
            staggered.ApplyControl(Control(DefenseCommandKind.GuardPress), 0);
            staggered.ResolveImpact(Strike(1), 100);
            staggered.ReleaseHeldDefense();
            Assert.That(staggered.State, Is.EqualTo(PlayerCombatState.Staggered));
            Assert.That(staggered.ActionEndsUs, Is.EqualTo(600100));
            Assert.That(staggered.Guard, Is.Zero);
        }

        [Test]
        public void ReleasedParryFinishesItsCommittedRecoveryWithoutAnImpact()
        {
            var player = new DefenseCombatant();
            player.TryParry(Strike(), 200000, SwipeDirection.Right, 60000);

            player.ReleaseHeldDefense();
            player.AdvanceTo(200001);

            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Recovery));
            Assert.That(player.ActionEndsUs, Is.EqualTo(450000));
            player.AdvanceTo(450000);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
            Assert.That(player.Health, Is.EqualTo(100));
            Assert.That(player.Guard, Is.EqualTo(100));
            Assert.That(player.DodgeCharges, Is.EqualTo(3));
        }

        [Test]
        public void GuardReleaseCommandCanClearHeldGuardDuringACommittedAction()
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.GuardPress), 0);

            Assert.That(player.ApplyControl(Control(DefenseCommandKind.GuardRelease), 1), Is.True);

            Assert.That(player.GuardHeld, Is.False);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
        }

        [Test]
        public void ForwardTimeValidationAndMalformedArgumentsNeverMutateState()
        {
            var player = new DefenseCombatant();
            player.AdvanceTo(100);

            Assert.Throws<ArgumentOutOfRangeException>(() => player.AdvanceTo(99));
            Assert.Throws<ArgumentOutOfRangeException>(() => player.ApplyControl(Control(DefenseCommandKind.GuardPress), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => player.ApplyControl(default, 200));
            Assert.Throws<ArgumentNullException>(() => player.TryParry(null, 200, SwipeDirection.Right, 200));
            Assert.Throws<ArgumentOutOfRangeException>(() => player.TryParry(Strike(), -1, SwipeDirection.Right, 200));
            Assert.Throws<ArgumentOutOfRangeException>(() => player.TryParry(Strike(), 200, (SwipeDirection)99, 200));
            Assert.Throws<ArgumentNullException>(() => player.ResolveImpact(null, 200));
            Assert.Throws<ArgumentOutOfRangeException>(() => player.ResolveImpact(Strike(), 99));
            Assert.That(player.TimeUs, Is.EqualTo(100));
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
            Assert.That(player.Health, Is.EqualTo(100));
            Assert.That(player.Guard, Is.EqualTo(100));
            Assert.That(player.DodgeCharges, Is.EqualTo(3));
        }

        [Test]
        public void TimerOverflowRejectsDodgeParryAndAliveHitBeforeMutation()
        {
            var player = new DefenseCombatant();

            Assert.Throws<OverflowException>(() => player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), long.MaxValue));
            Assert.Throws<OverflowException>(() => player.TryParry(Strike(), long.MaxValue, SwipeDirection.Right, long.MaxValue));
            Assert.Throws<OverflowException>(() => player.ResolveImpact(Strike(), long.MaxValue));
            Assert.That(player.TimeUs, Is.Zero);
            Assert.That(player.DodgeCharges, Is.EqualTo(3));
            Assert.That(player.Health, Is.EqualTo(100));
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
        }

        [Test]
        public void RechargeCanAdvanceToMaximumTimeWithoutLoopingOrOverflowing()
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 0);

            player.AdvanceTo(long.MaxValue);

            Assert.That(player.DodgeCharges, Is.EqualTo(3));
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
            Assert.That(player.TimeUs, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void TuningRejectsInvalidResourceAndTimingRelationships()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatDefenseTuning(maximumHealth: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatDefenseTuning(maximumGuard: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatDefenseTuning(maximumDodgeCharges: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatDefenseTuning(guardStaggerUs: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatDefenseTuning(dodgeDurationUs: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatDefenseTuning(dodgeAvoidanceStartUs: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatDefenseTuning(dodgeAvoidanceStartUs: 230001));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatDefenseTuning(dodgeAvoidanceEndUs: 360000));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatDefenseTuning(parryWindowUs: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatDefenseTuning(parryRecoveryUs: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatDefenseTuning(hitRecoveryUs: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatDefenseTuning(dodgeRechargeUs: 0));
        }

        [Test]
        public void CustomTuningControlsResourcesAndActionBoundaries()
        {
            var tuning = new CombatDefenseTuning(80, 60, 2, 100, 40, 5, 20, 14, 25, 20, 300);
            var player = new DefenseCombatant(tuning);
            Assert.That(player.Tuning, Is.SameAs(tuning));
            Assert.That(player.Health, Is.EqualTo(80));
            Assert.That(player.Guard, Is.EqualTo(60));
            Assert.That(player.DodgeCharges, Is.EqualTo(2));
            player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 0);
            Assert.That(player.ResolveImpact(Strike(), 5).Outcome, Is.EqualTo(DefenseOutcome.Dodged));
            player.AdvanceTo(21);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Recovery));
            player.AdvanceTo(40);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
            player.AdvanceTo(300);
            Assert.That(player.DodgeCharges, Is.EqualTo(2));
        }

        private static DefenseCombatant PlayerInState(PlayerCombatState state)
        {
            var player = new DefenseCombatant(state == PlayerCombatState.Staggered
                ? new CombatDefenseTuning(maximumGuard: 25) : null);
            switch (state)
            {
                case PlayerCombatState.Dodging:
                    player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 0);
                    break;
                case PlayerCombatState.Parrying:
                    player.TryParry(Strike(), 100000, SwipeDirection.Right, 0);
                    break;
                case PlayerCombatState.Recovery:
                    player.ResolveImpact(Strike(), 0);
                    break;
                case PlayerCombatState.Staggered:
                    player.ApplyControl(Control(DefenseCommandKind.GuardPress), 0);
                    player.ResolveImpact(Strike(), 0);
                    break;
                case PlayerCombatState.Dead:
                    player.ResolveImpact(Strike(1, damage: 100), 0);
                    break;
            }
            return player;
        }
    }
}
