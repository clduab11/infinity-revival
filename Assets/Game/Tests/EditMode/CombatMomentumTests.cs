using System;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class CombatMomentumTests
    {
        [Test]
        public void NewEncounterStartsWithNoPressureFocusOrPendingBreak()
        {
            var momentum = new CombatMomentum();

            Assert.That(momentum.TimeUs, Is.Zero);
            Assert.That(momentum.Balance, Is.Zero);
            Assert.That(momentum.Focus, Is.Zero);
            Assert.That(momentum.PendingBalanceBreak, Is.False);
            Assert.That(momentum.ConsumeBalanceBreak(), Is.False);
        }

        [TestCase(DefenseOutcome.Parried, 25)]
        [TestCase(DefenseOutcome.Dodged, 10)]
        [TestCase(DefenseOutcome.Blocked, 5)]
        [TestCase(DefenseOutcome.Hit, 0)]
        [TestCase(DefenseOutcome.GuardBroken, 0)]
        [TestCase(DefenseOutcome.IgnoredAfterDeath, 0)]
        public void OnlySuccessfulDefenseGrantsAuthoredPressureAndFocus(DefenseOutcome outcome, int gain)
        {
            var momentum = new CombatMomentum();

            momentum.RecordDefense(Resolution(outcome, 123456));

            Assert.That(momentum.TimeUs, Is.EqualTo(123456));
            Assert.That(momentum.Balance, Is.EqualTo(gain));
            Assert.That(momentum.Focus, Is.EqualTo(gain));
            Assert.That(momentum.PendingBalanceBreak, Is.False);
        }

        [Test]
        public void ExactThresholdResetsPressureAndPublishesOneConsumableBreak()
        {
            var momentum = new CombatMomentum();
            for (int index = 0; index < 3; index++)
                momentum.RecordDefense(Resolution(DefenseOutcome.Parried));
            momentum.RecordDefense(Resolution(DefenseOutcome.Dodged));
            momentum.RecordDefense(Resolution(DefenseOutcome.Dodged));

            Assert.That(momentum.Balance, Is.EqualTo(95));
            Assert.That(momentum.PendingBalanceBreak, Is.False);
            momentum.RecordDefense(Resolution(DefenseOutcome.Blocked));

            Assert.That(momentum.Balance, Is.Zero);
            Assert.That(momentum.Focus, Is.EqualTo(100));
            Assert.That(momentum.PendingBalanceBreak, Is.True);
            Assert.That(momentum.ConsumeBalanceBreak(), Is.True);
            Assert.That(momentum.PendingBalanceBreak, Is.False);
            Assert.That(momentum.ConsumeBalanceBreak(), Is.False);
        }

        [Test]
        public void ThresholdOverflowResetsPressureWithoutCarryingExcessAndKeepsFocus()
        {
            var momentum = new CombatMomentum(new CombatOffenseTuning(balanceMaximum: 30));

            momentum.RecordDefense(Resolution(DefenseOutcome.Parried));
            momentum.RecordDefense(Resolution(DefenseOutcome.Dodged));

            Assert.That(momentum.Balance, Is.Zero);
            Assert.That(momentum.Focus, Is.EqualTo(35));
            Assert.That(momentum.PendingBalanceBreak, Is.True);
        }

        [Test]
        public void EqualTimeDefensesEachGrantResourcesWithoutAdvancingTime()
        {
            var momentum = new CombatMomentum();

            momentum.RecordDefense(Resolution(DefenseOutcome.Parried, 500000));
            momentum.RecordDefense(Resolution(DefenseOutcome.Dodged, 500000));
            momentum.RecordDefense(Resolution(DefenseOutcome.Blocked, 500000));

            Assert.That(momentum.TimeUs, Is.EqualTo(500000));
            Assert.That(momentum.Balance, Is.EqualTo(40));
            Assert.That(momentum.Focus, Is.EqualTo(40));
        }

        [Test]
        public void FocusSaturatesIndependentlyOfRepeatedBalanceBreaks()
        {
            var momentum = new CombatMomentum();
            for (int index = 0; index < 9; index++)
                momentum.RecordDefense(Resolution(DefenseOutcome.Parried));

            Assert.That(momentum.Balance, Is.EqualTo(25));
            Assert.That(momentum.Focus, Is.EqualTo(100));
            Assert.That(momentum.PendingBalanceBreak, Is.True);
            momentum.AdvanceTo(20000000);
            Assert.That(momentum.Balance, Is.Zero);
            Assert.That(momentum.Focus, Is.EqualTo(100));
            Assert.That(momentum.PendingBalanceBreak, Is.True);
        }

        [TestCase(2999999, 25)]
        [TestCase(3000000, 25)]
        [TestCase(3099999, 25)]
        [TestCase(3100000, 24)]
        [TestCase(3199999, 24)]
        [TestCase(3200000, 23)]
        public void DecayUsesExactIntegerDelayAndStepBoundaries(long elapsedUs, int balance)
        {
            var momentum = new CombatMomentum();
            momentum.RecordDefense(Resolution(DefenseOutcome.Parried, 17));

            momentum.AdvanceTo(17 + elapsedUs);

            Assert.That(momentum.Balance, Is.EqualTo(balance));
            Assert.That(momentum.Focus, Is.EqualTo(25));
        }

        [Test]
        public void PartialStepsAccumulateAndEqualTimeAdvanceNeverDoubleCounts()
        {
            var momentum = new CombatMomentum();
            momentum.RecordDefense(Resolution(DefenseOutcome.Parried));

            momentum.AdvanceTo(3099999);
            momentum.AdvanceTo(3100000);
            momentum.AdvanceTo(3100000);
            momentum.AdvanceTo(3150000);
            Assert.That(momentum.Balance, Is.EqualTo(24));
            momentum.AdvanceTo(3200000);

            Assert.That(momentum.Balance, Is.EqualTo(23));
        }

        [Test]
        public void DefenseFirstAppliesElapsedDecayThenStartsAFreshDelay()
        {
            var momentum = new CombatMomentum();
            momentum.RecordDefense(Resolution(DefenseOutcome.Parried));

            momentum.RecordDefense(Resolution(DefenseOutcome.Blocked, 3150000));
            Assert.That(momentum.Balance, Is.EqualTo(29));
            momentum.AdvanceTo(6249999);
            Assert.That(momentum.Balance, Is.EqualTo(29));
            momentum.AdvanceTo(6250000);

            Assert.That(momentum.Balance, Is.EqualTo(28));
            Assert.That(momentum.Focus, Is.EqualTo(30));
        }

        [TestCase(DefenseOutcome.Hit)]
        [TestCase(DefenseOutcome.GuardBroken)]
        public void FailedDefenseDoesNotRestartTheDecayDelay(DefenseOutcome outcome)
        {
            var momentum = new CombatMomentum();
            momentum.RecordDefense(Resolution(DefenseOutcome.Parried));

            momentum.RecordDefense(Resolution(outcome, 2999999));
            momentum.AdvanceTo(3100000);

            Assert.That(momentum.Balance, Is.EqualTo(24));
            Assert.That(momentum.Focus, Is.EqualTo(25));
        }

        [Test]
        public void DecayClampsAtZeroEvenAfterTheLargestSupportedTimeJump()
        {
            var momentum = new CombatMomentum();
            momentum.RecordDefense(Resolution(DefenseOutcome.Parried));

            momentum.AdvanceTo(long.MaxValue);
            momentum.AdvanceTo(long.MaxValue);

            Assert.That(momentum.TimeUs, Is.EqualTo(long.MaxValue));
            Assert.That(momentum.Balance, Is.Zero);
            Assert.That(momentum.Focus, Is.EqualTo(25));
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void FramePartitionsProduceTheSameIntegerResourcesAsOneJump(int framesPerSecond)
        {
            const long endUs = 4250000;
            var partitioned = new CombatMomentum();
            var direct = new CombatMomentum();
            partitioned.RecordDefense(Resolution(DefenseOutcome.Parried));
            direct.RecordDefense(Resolution(DefenseOutcome.Parried));
            for (long frame = 1; frame * 1000000 / framesPerSecond < endUs; frame++)
                partitioned.AdvanceTo(frame * 1000000 / framesPerSecond);

            partitioned.AdvanceTo(endUs);
            direct.AdvanceTo(endUs);

            Assert.That(partitioned.TimeUs, Is.EqualTo(direct.TimeUs));
            Assert.That(partitioned.Balance, Is.EqualTo(13));
            Assert.That(partitioned.Balance, Is.EqualTo(direct.Balance));
            Assert.That(partitioned.Focus, Is.EqualTo(direct.Focus));
            Assert.That(partitioned.PendingBalanceBreak, Is.EqualTo(direct.PendingBalanceBreak));
        }

        [Test]
        public void JitterPartitionsDoNotLoseFractionalDecaySteps()
        {
            var momentum = new CombatMomentum();
            momentum.RecordDefense(Resolution(DefenseOutcome.Parried));
            long timeUs = 0;
            long[] steps = { 713, 41003, 120007, 99991, 7, 16001 };
            int index = 0;
            while (timeUs < 4250000)
            {
                timeUs = Math.Min(4250000, timeUs + steps[index++ % steps.Length]);
                momentum.AdvanceTo(timeUs);
            }

            Assert.That(momentum.Balance, Is.EqualTo(13));
            Assert.That(momentum.Focus, Is.EqualTo(25));
        }

        [Test]
        public void AuthoredPressureFocusAndDecayAreIndependent()
        {
            var momentum = new CombatMomentum(new CombatOffenseTuning(balanceMaximum: 200,
                balanceDecayDelayUs: 10, balanceDecayStepUs: 3, focusMaximum: 9,
                parryBalance: 7, dodgeBalance: 4, blockBalance: 2,
                parryFocus: 6, dodgeFocus: 2, blockFocus: 1));
            momentum.RecordDefense(Resolution(DefenseOutcome.Parried));
            momentum.RecordDefense(Resolution(DefenseOutcome.Dodged));
            momentum.RecordDefense(Resolution(DefenseOutcome.Blocked));

            momentum.AdvanceTo(16);

            Assert.That(momentum.Balance, Is.EqualTo(11));
            Assert.That(momentum.Focus, Is.EqualTo(9));
        }

        [Test]
        public void ZeroPressureGrantStillAwardsItsIndependentFocusGrant()
        {
            var momentum = new CombatMomentum(new CombatOffenseTuning(parryBalance: 0, parryFocus: 7));

            momentum.RecordDefense(Resolution(DefenseOutcome.Parried));

            Assert.That(momentum.Balance, Is.Zero);
            Assert.That(momentum.Focus, Is.EqualTo(7));
            Assert.That(momentum.PendingBalanceBreak, Is.False);
        }

        [Test]
        public void LargeAuthoredGrantsNeverOverflowEitherResource()
        {
            var momentum = new CombatMomentum(new CombatOffenseTuning(balanceMaximum: int.MaxValue,
                focusMaximum: int.MaxValue, parryBalance: int.MaxValue - 1,
                blockBalance: 10, parryFocus: int.MaxValue - 1, blockFocus: 10));
            momentum.RecordDefense(Resolution(DefenseOutcome.Parried));

            momentum.RecordDefense(Resolution(DefenseOutcome.Blocked));

            Assert.That(momentum.Balance, Is.Zero);
            Assert.That(momentum.Focus, Is.EqualTo(int.MaxValue));
            Assert.That(momentum.PendingBalanceBreak, Is.True);
        }

        [Test]
        public void DefenseAtMaximumTimeDoesNotOverflowTheDecayOrigin()
        {
            var momentum = new CombatMomentum();

            momentum.RecordDefense(Resolution(DefenseOutcome.Blocked, long.MaxValue));
            momentum.AdvanceTo(long.MaxValue);

            Assert.That(momentum.Balance, Is.EqualTo(5));
            Assert.That(momentum.Focus, Is.EqualTo(5));
        }

        [Test]
        public void MaximumDecayDelayUsesElapsedTimeWithoutOverflowingAnAbsoluteDeadline()
        {
            var momentum = new CombatMomentum(new CombatOffenseTuning(
                balanceDecayDelayUs: long.MaxValue, balanceDecayStepUs: 1));
            momentum.RecordDefense(Resolution(DefenseOutcome.Blocked, 1));

            momentum.AdvanceTo(long.MaxValue);

            Assert.That(momentum.Balance, Is.EqualTo(5));
            Assert.That(momentum.Focus, Is.EqualTo(5));
        }

        [Test]
        public void NegativeAndRegressingTimesRejectWithoutMutatingResources()
        {
            var momentum = new CombatMomentum();
            momentum.RecordDefense(Resolution(DefenseOutcome.Parried, 500000));

            Assert.Throws<ArgumentOutOfRangeException>(() => momentum.AdvanceTo(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => momentum.AdvanceTo(499999));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                momentum.RecordDefense(Resolution(DefenseOutcome.Blocked, 499999)));

            Assert.That(momentum.TimeUs, Is.EqualTo(500000));
            Assert.That(momentum.Balance, Is.EqualTo(25));
            Assert.That(momentum.Focus, Is.EqualTo(25));
        }

        [Test]
        public void LethalResolutionEndsFutureResourceGainAndDecay()
        {
            var momentum = new CombatMomentum();
            momentum.RecordDefense(Resolution(DefenseOutcome.Parried));
            var player = new DefenseCombatant();
            var lethal = player.ResolveImpact(Strike(healthDamage: 150), 1);

            momentum.RecordDefense(lethal);
            momentum.RecordDefense(Resolution(DefenseOutcome.Parried, 2));
            momentum.AdvanceTo(20000000);

            Assert.That(momentum.TimeUs, Is.EqualTo(20000000));
            Assert.That(momentum.Balance, Is.EqualTo(25));
            Assert.That(momentum.Focus, Is.EqualTo(25));
        }

        [Test]
        public void StopIsTerminalIdempotentAndRetainsResourcesAndPendingBreak()
        {
            var momentum = new CombatMomentum();
            for (int index = 0; index < 4; index++)
                momentum.RecordDefense(Resolution(DefenseOutcome.Parried));
            momentum.RecordDefense(Resolution(DefenseOutcome.Blocked));

            momentum.Stop();
            momentum.Stop();
            momentum.RecordDefense(Resolution(DefenseOutcome.Parried, 1));
            momentum.AdvanceTo(20000000);

            Assert.That(momentum.TimeUs, Is.EqualTo(20000000));
            Assert.That(momentum.Balance, Is.EqualTo(5));
            Assert.That(momentum.Focus, Is.EqualTo(100));
            Assert.That(momentum.PendingBalanceBreak, Is.True);
            Assert.That(momentum.ConsumeBalanceBreak(), Is.True);
            Assert.That(momentum.PendingBalanceBreak, Is.False);
        }

        private static DefenseResolution Resolution(DefenseOutcome outcome, long timeUs = 0)
        {
            var tuning = new CombatDefenseTuning(maximumGuard: outcome == DefenseOutcome.GuardBroken ? 20 : 100,
                guardStaggerUs: 1, dodgeDurationUs: 2, dodgeAvoidanceStartUs: 0,
                dodgeAvoidanceEndUs: 1, parryRecoveryUs: 1, hitRecoveryUs: 1, dodgeRechargeUs: 1);
            var player = new DefenseCombatant(tuning);
            var strike = Strike(healthDamage: outcome == DefenseOutcome.IgnoredAfterDeath ? 150 : 30);
            if (outcome == DefenseOutcome.Parried)
                Assert.That(player.TryParry(strike, timeUs, SwipeDirection.Right, timeUs), Is.True);
            else if (outcome == DefenseOutcome.Dodged)
                Assert.That(player.ApplyControl(new DefenseCommand(1, timeUs, DefenseCommandKind.DodgeLeft), timeUs), Is.True);
            else if (outcome == DefenseOutcome.Blocked || outcome == DefenseOutcome.GuardBroken)
                Assert.That(player.ApplyControl(new DefenseCommand(1, timeUs, DefenseCommandKind.GuardPress), timeUs), Is.True);
            else if (outcome == DefenseOutcome.IgnoredAfterDeath)
                player.ResolveImpact(strike, timeUs);
            var resolution = player.ResolveImpact(strike, timeUs);
            Assert.That(resolution.Outcome, Is.EqualTo(outcome));
            return resolution;
        }

        private static EnemyStrike Strike(int healthDamage = 30)
        {
            return new EnemyStrike(1, DefenseMask.Guard | DefenseMask.Parry | DefenseMask.Dodge,
                DodgeSide.Both, SwipeDirection.Right, 25, healthDamage);
        }
    }
}
