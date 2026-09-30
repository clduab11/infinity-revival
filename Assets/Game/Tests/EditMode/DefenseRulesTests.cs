using System;
using System.Collections.Generic;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class DefenseRulesTests
    {
        [TestCase(1, DefenseOutcome.Blocked)]
        [TestCase(2, DefenseOutcome.Hit)]
        [TestCase(3, DefenseOutcome.Blocked)]
        [TestCase(4, DefenseOutcome.Hit)]
        [TestCase(5, DefenseOutcome.Blocked)]
        [TestCase(6, DefenseOutcome.Hit)]
        [TestCase(7, DefenseOutcome.Blocked)]
        public void GuardPermissionIsAuthoredPerStrike(int mask, DefenseOutcome outcome)
        {
            var player = new DefenseCombatant();
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.GuardPress), 0), Is.True);

            var result = player.ResolveImpact(Strike(mask), 100000);

            Assert.That(result.Outcome, Is.EqualTo(outcome));
            Assert.That(result.HealthDamage, Is.EqualTo(outcome == DefenseOutcome.Blocked ? 0 : 30));
            Assert.That(result.GuardSpent, Is.EqualTo(outcome == DefenseOutcome.Blocked ? 25 : 0));
            Assert.That(player.Health, Is.EqualTo(outcome == DefenseOutcome.Blocked ? 100 : 70));
        }

        [TestCaseSource(nameof(ParryCases))]
        public void EveryMaskUsesItsAuthoredCardinalParry(int mask, bool permitted, SwipeDirection direction)
        {
            var player = new DefenseCombatant();
            var strike = Strike(mask, direction: direction);

            Assert.That(player.TryParry(strike, 200000, direction, 60000), Is.EqualTo(permitted));
            var result = player.ResolveImpact(strike, 200000);

            Assert.That(result.Outcome, Is.EqualTo(permitted ? DefenseOutcome.Parried : DefenseOutcome.Hit));
            Assert.That(result.HealthDamage, Is.EqualTo(permitted ? 0 : 30));
            Assert.That(result.GuardSpent, Is.Zero);
        }

        [TestCaseSource(nameof(DodgeCases))]
        public void EveryMaskUsesBothAuthoredSafeDodgeSides(int mask, bool permitted, DefenseCommandKind kind)
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(kind), 0);

            var result = player.ResolveImpact(Strike(mask), 100000);

            Assert.That(result.Outcome, Is.EqualTo(permitted ? DefenseOutcome.Dodged : DefenseOutcome.Hit));
            Assert.That(player.DodgeCharges, Is.EqualTo(2));
            Assert.That(result.HealthDamage, Is.EqualTo(permitted ? 0 : 30));
        }

        [TestCaseSource(nameof(SafeDodgeCases))]
        public void DodgeMustUseAnAuthoredSafeSide(DodgeSide safeSide,
            DefenseCommandKind control, DefenseOutcome expected, int mask)
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(control), 0);

            Assert.That(player.ResolveImpact(Strike(mask, safeSide), 100000).Outcome, Is.EqualTo(expected));
        }

        [TestCase(SwipeDirection.Left, SwipeDirection.Right)]
        [TestCase(SwipeDirection.Left, SwipeDirection.Up)]
        [TestCase(SwipeDirection.Left, SwipeDirection.Down)]
        [TestCase(SwipeDirection.Right, SwipeDirection.Left)]
        [TestCase(SwipeDirection.Right, SwipeDirection.Up)]
        [TestCase(SwipeDirection.Right, SwipeDirection.Down)]
        [TestCase(SwipeDirection.Up, SwipeDirection.Left)]
        [TestCase(SwipeDirection.Up, SwipeDirection.Right)]
        [TestCase(SwipeDirection.Up, SwipeDirection.Down)]
        [TestCase(SwipeDirection.Down, SwipeDirection.Left)]
        [TestCase(SwipeDirection.Down, SwipeDirection.Right)]
        [TestCase(SwipeDirection.Down, SwipeDirection.Up)]
        public void WrongParryDirectionsDoNotCommitRecovery(SwipeDirection required, SwipeDirection entered)
        {
            var player = new DefenseCombatant();
            var strike = Strike(7, direction: required);

            Assert.That(player.TryParry(strike, 200000, entered, 100000), Is.False);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.GuardPress), 100000), Is.True);
            Assert.That(player.ResolveImpact(strike, 200000).Outcome, Is.EqualTo(DefenseOutcome.Blocked));
        }

        [Test]
        public void ParryTakesPriorityOverPreviouslyHeldGuard()
        {
            var player = new DefenseCombatant();
            var strike = Strike(7);
            player.ApplyControl(Control(DefenseCommandKind.GuardPress), 0);

            Assert.That(player.TryParry(strike, 200000, SwipeDirection.Right, 100000), Is.True);
            var result = player.ResolveImpact(strike, 200000);

            Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.Parried));
            Assert.That(player.Guard, Is.EqualTo(100));
            Assert.That(player.GuardHeld, Is.False);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Recovery));
        }

        [Test]
        public void ParryIsBoundToStrikeIdentityAndImpactTime()
        {
            var player = new DefenseCombatant();
            player.TryParry(Strike(7, id: 1), 200000, SwipeDirection.Right, 100000);

            var result = player.ResolveImpact(Strike(7, id: 2), 200000);

            Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.Hit));
            Assert.That(player.ResolveImpact(Strike(7, id: 1), 200000).Outcome, Is.EqualTo(DefenseOutcome.Hit));
        }

        [Test]
        public void PrematureImpactDoesNotConsumeAnArmedParryAsSuccess()
        {
            var player = new DefenseCombatant();
            var strike = Strike(7);
            player.TryParry(strike, 200000, SwipeDirection.Right, 100000);

            Assert.That(player.ResolveImpact(strike, 199999).Outcome, Is.EqualTo(DefenseOutcome.Hit));
            Assert.That(player.ResolveImpact(strike, 200000).Outcome, Is.EqualTo(DefenseOutcome.Hit));
        }

        [Test]
        public void ValidDodgeConsumesChargeImmediatelyAndClearsGuard()
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.GuardPress), 0);

            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 100), Is.True);

            Assert.That(player.GuardHeld, Is.False);
            Assert.That(player.Guard, Is.EqualTo(100));
            Assert.That(player.DodgeCharges, Is.EqualTo(2));
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Dodging));
            Assert.That(player.DodgeSide, Is.EqualTo(DodgeSide.Left));
            Assert.That(player.DodgeStartedUs, Is.EqualTo(100));
            Assert.That(player.ActionEndsUs, Is.EqualTo(360100));
        }

        [Test]
        public void GuardBreakAbsorbsBreakingStrikeAndLocksFollowingDefense()
        {
            var player = new DefenseCombatant(new CombatDefenseTuning(maximumGuard: 20));
            player.ApplyControl(Control(DefenseCommandKind.GuardPress), 0);

            var broken = player.ResolveImpact(Strike(1), 100000);

            Assert.That(broken.Outcome, Is.EqualTo(DefenseOutcome.GuardBroken));
            Assert.That(broken.GuardSpent, Is.EqualTo(20));
            Assert.That(broken.HealthDamage, Is.Zero);
            Assert.That(player.Health, Is.EqualTo(100));
            Assert.That(player.Guard, Is.Zero);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Staggered));
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.GuardPress), 200000), Is.False);
            var hit = player.ResolveImpact(Strike(1, id: 2), 200000);
            Assert.That(hit.Outcome, Is.EqualTo(DefenseOutcome.Hit));
            Assert.That(hit.HealthAfter, Is.EqualTo(70));
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Staggered));
            Assert.That(player.ActionEndsUs, Is.EqualTo(700000));
        }

        [Test]
        public void HitCancelsDodgeAndParryAndAppliesFullAuthoredDamage()
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 0);

            var result = player.ResolveImpact(Strike(1, damage: 37), 100000);

            Assert.That(result.StrikeId, Is.EqualTo(1));
            Assert.That(result.TimeUs, Is.EqualTo(100000));
            Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.Hit));
            Assert.That(result.HealthDamage, Is.EqualTo(37));
            Assert.That(result.HealthAfter, Is.EqualTo(63));
            Assert.That(result.GuardAfter, Is.EqualTo(100));
            Assert.That(result.IsDead, Is.False);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Recovery));
            Assert.That(player.DodgeSide, Is.EqualTo(DodgeSide.None));
            Assert.That(player.DodgeCharges, Is.EqualTo(2));
        }

        [Test]
        public void LethalImpactSaturatesHealthAndAllLaterInputsAndImpactsStayDead()
        {
            var player = new DefenseCombatant();
            player.ApplyControl(Control(DefenseCommandKind.DodgeLeft), 0);
            var lethal = player.ResolveImpact(Strike(1, damage: 150), 1);

            Assert.That(lethal.HealthDamage, Is.EqualTo(150));
            Assert.That(lethal.HealthAfter, Is.Zero);
            Assert.That(lethal.IsDead, Is.True);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Dead));
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.GuardPress), 1), Is.False);
            Assert.That(player.ApplyControl(Control(DefenseCommandKind.DodgeRight), 1), Is.False);
            Assert.That(player.TryParry(Strike(7), 1, SwipeDirection.Right, 1), Is.False);
            var second = player.ResolveImpact(Strike(7, id: 2), 1);
            Assert.That(second.Outcome, Is.EqualTo(DefenseOutcome.IgnoredAfterDeath));
            Assert.That(second.HealthDamage, Is.Zero);
            Assert.That(second.GuardSpent, Is.Zero);
            player.AdvanceTo(10000000);
            Assert.That(player.DodgeCharges, Is.EqualTo(2));
            Assert.That(player.Health, Is.Zero);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Dead));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void CommandsRejectNonpositiveSequence(long sequence)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DefenseCommand(sequence, 0, DefenseCommandKind.GuardPress));
        }

        [Test]
        public void CommandsRejectMalformedTimestampAndKind()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DefenseCommand(1, -1, DefenseCommandKind.GuardPress));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DefenseCommand(1, 0, (DefenseCommandKind)99));
        }

        [TestCase(0)]
        [TestCase(8)]
        [TestCase(-1)]
        public void StrikesRejectEmptyAndUnknownDefenseMasks(int mask)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyStrike(1, (DefenseMask)mask,
                DodgeSide.None, SwipeDirection.Right, 25, 30));
        }

        [TestCase(4, DodgeSide.None)]
        [TestCase(4, (DodgeSide)4)]
        [TestCase(1, DodgeSide.Left)]
        [TestCase(2, DodgeSide.Both)]
        public void StrikesRejectInconsistentSafeSides(int mask, DodgeSide safeSide)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyStrike(1, (DefenseMask)mask,
                safeSide, SwipeDirection.Right, 25, 30));
        }

        [Test]
        public void StrikesRejectMalformedIdentityDamageCostDirectionAndDurations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyStrike(0, DefenseMask.Guard, DodgeSide.None, SwipeDirection.Right, 25, 30));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyStrike(1, DefenseMask.Guard, DodgeSide.None, SwipeDirection.Right, 0, 30));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyStrike(1, DefenseMask.Parry, DodgeSide.None, SwipeDirection.Right, -1, 30));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyStrike(1, DefenseMask.Parry, DodgeSide.None, SwipeDirection.Right, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyStrike(1, DefenseMask.Parry, DodgeSide.None, (SwipeDirection)99, 0, 30));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyStrike(1, DefenseMask.Parry, DodgeSide.None, SwipeDirection.Right, 0, 30, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyStrike(1, DefenseMask.Parry, DodgeSide.None, SwipeDirection.Right, 0, 30, recoveryDurationUs: -1));
        }

        [Test]
        public void StrikesPreserveZeroDamageAndInstantRecoveryAuthoredValues()
        {
            var strike = new EnemyStrike(9, DefenseMask.Parry, DodgeSide.None,
                SwipeDirection.Up, 0, 0, 1, 0, true);

            Assert.That(strike.Id, Is.EqualTo(9));
            Assert.That(strike.AllowedDefenses, Is.EqualTo(DefenseMask.Parry));
            Assert.That(strike.SafeDodgeSides, Is.EqualTo(DodgeSide.None));
            Assert.That(strike.RequiredParryDirection, Is.EqualTo(SwipeDirection.Up));
            Assert.That(strike.GuardCost, Is.Zero);
            Assert.That(strike.HealthDamage, Is.Zero);
            Assert.That(strike.TelegraphDurationUs, Is.EqualTo(1));
            Assert.That(strike.RecoveryDurationUs, Is.Zero);
            Assert.That(strike.Interruptible, Is.True);
        }

        private static IEnumerable<TestCaseData> ParryCases()
        {
            bool[] permitted = { false, true, true, false, false, true, true };
            SwipeDirection[] directions = { SwipeDirection.Left, SwipeDirection.Right,
                SwipeDirection.Up, SwipeDirection.Down };
            for (int index = 0; index < 7; index++)
                foreach (var direction in directions)
                    yield return new TestCaseData(index + 1, permitted[index], direction);
        }

        private static IEnumerable<TestCaseData> DodgeCases()
        {
            bool[] permitted = { false, false, false, true, true, true, true };
            DefenseCommandKind[] controls = { DefenseCommandKind.DodgeLeft, DefenseCommandKind.DodgeRight };
            for (int index = 0; index < 7; index++)
                foreach (var control in controls)
                    yield return new TestCaseData(index + 1, permitted[index], control);
        }

        private static IEnumerable<TestCaseData> SafeDodgeCases()
        {
            object[][] cases = {
                new object[] { DodgeSide.Left, DefenseCommandKind.DodgeLeft, DefenseOutcome.Dodged },
                new object[] { DodgeSide.Left, DefenseCommandKind.DodgeRight, DefenseOutcome.Hit },
                new object[] { DodgeSide.Right, DefenseCommandKind.DodgeLeft, DefenseOutcome.Hit },
                new object[] { DodgeSide.Right, DefenseCommandKind.DodgeRight, DefenseOutcome.Dodged },
                new object[] { DodgeSide.Both, DefenseCommandKind.DodgeLeft, DefenseOutcome.Dodged },
                new object[] { DodgeSide.Both, DefenseCommandKind.DodgeRight, DefenseOutcome.Dodged }
            };
            foreach (var row in cases)
                foreach (int mask in new[] { 4, 5, 6, 7 })
                    yield return new TestCaseData(row[0], row[1], row[2], mask);
        }

        internal static DefenseCommand Control(DefenseCommandKind kind, long sequence = 1)
        {
            return new DefenseCommand(sequence, 0, kind);
        }

        internal static EnemyStrike Strike(int mask = 7, DodgeSide safeSide = DodgeSide.Both,
            SwipeDirection direction = SwipeDirection.Right, long id = 1, int damage = 30)
        {
            return new EnemyStrike(id, (DefenseMask)mask,
                (mask & 4) != 0 ? safeSide : DodgeSide.None, direction, 25, damage);
        }
    }
}
