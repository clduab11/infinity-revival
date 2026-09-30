using System;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class OffenseRulesTests
    {
        [TestCase(SwipeDirection.Left)]
        [TestCase(SwipeDirection.Right)]
        [TestCase(SwipeDirection.Up)]
        [TestCase(SwipeDirection.Down)]
        public void OpeningSwipeCommitsAnAuthoredImpactAndSharedRecovery(SwipeDirection direction)
        {
            var player = new DefenseCombatant();
            var offense = Open(player);

            var result = offense.RequestAttack(Command(direction), 0);

            Assert.That(result.Status, Is.EqualTo(AttackRequestStatus.Started));
            Assert.That(result.Attack.HasValue, Is.True);
            var attack = result.Attack.Value;
            Assert.That(attack.Id, Is.EqualTo(1));
            Assert.That(attack.Phase.Id, Is.EqualTo(2));
            Assert.That(attack.Direction, Is.EqualTo(direction));
            Assert.That(attack.StartedUs, Is.Zero);
            Assert.That(attack.ImpactUs, Is.EqualTo(100000));
            Assert.That(attack.RecoveryEndUs, Is.EqualTo(500000));
            Assert.That(attack.Damage, Is.EqualTo(20));
            Assert.That(attack.ComboFinisher, Is.False);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Attacking));
            Assert.That(offense.EnemyHealth, Is.EqualTo(100));
        }

        [Test]
        public void GuardedOpeningAttackReleasesGuardAndLocksEveryDefense()
        {
            var player = new DefenseCombatant();
            player.ApplyControl(DefenseRulesTests.Control(DefenseCommandKind.GuardPress), 0);
            var offense = Open(player);
            offense.RequestAttack(Command(SwipeDirection.Left), 0);

            Assert.That(player.GuardHeld, Is.False);
            Assert.That(player.ApplyControl(DefenseRulesTests.Control(DefenseCommandKind.GuardPress), 1), Is.False);
            Assert.That(player.ApplyControl(DefenseRulesTests.Control(DefenseCommandKind.DodgeLeft), 1), Is.False);
            Assert.That(player.TryParry(DefenseRulesTests.Strike(), 100000, SwipeDirection.Right, 1), Is.False);
            offense.AdvanceTo(100000);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Recovery));
            Assert.That(player.ApplyControl(DefenseRulesTests.Control(DefenseCommandKind.DodgeRight), 499999), Is.False);
            offense.AdvanceTo(500000);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
        }

        [Test]
        public void AttackAtExactRecoveryBoundaryCanReplaceTheCompletedRecord()
        {
            var offense = Open(new DefenseCombatant());
            var first = offense.RequestAttack(Command(SwipeDirection.Left), 0).Attack.Value;
            offense.ResolveImpact(first.Id, 100000);

            var next = offense.RequestAttack(Command(SwipeDirection.Right, 500000), 500000);
            offense.CompleteRecovery(first.Id, 500000);

            Assert.That(next.Status, Is.EqualTo(AttackRequestStatus.Started));
            Assert.That(next.Attack.Value.Id, Is.EqualTo(2));
            Assert.That(offense.ActiveAttack.Value.Id, Is.EqualTo(2));
        }

        [TestCase(99999, AttackRequestStatus.Rejected)]
        [TestCase(100000, AttackRequestStatus.Rejected)]
        [TestCase(100001, AttackRequestStatus.Started)]
        public void ImpactMustFitStrictlyBeforeTheOpeningEnd(long durationUs, AttackRequestStatus status)
        {
            var offense = Open(new DefenseCombatant(), durationUs: durationUs);

            Assert.That(offense.RequestAttack(Command(SwipeDirection.Left), 0).Status, Is.EqualTo(status));
            Assert.That(offense.Player.State, Is.EqualTo(status == AttackRequestStatus.Started
                ? PlayerCombatState.Attacking : PlayerCombatState.Ready));
        }

        [Test]
        public void OpeningIsHalfOpenAndInactiveRequestsDoNotBuffer()
        {
            var offense = new OffenseCombatant(new DefenseCombatant());
            Assert.That(offense.RequestAttack(Command(SwipeDirection.Left), 0).Status,
                Is.EqualTo(AttackRequestStatus.Rejected));
            offense.StartOpening(Phase(), 10, 1000000);
            Assert.That(offense.OpeningStartUs, Is.EqualTo(10));
            Assert.That(offense.OpeningEndUs, Is.EqualTo(1000010));
            Assert.That(offense.IsOpening, Is.True);

            Assert.That(offense.RequestAttack(Command(SwipeDirection.Left, 1000010), 1000010).Status,
                Is.EqualTo(AttackRequestStatus.Rejected));
            Assert.That(offense.IsOpening, Is.False);
            Assert.That(offense.HasBuffer, Is.False);
        }

        [Test]
        public void CapturedEnemyAndOldOpeningCommandsCannotBecomeAttacks()
        {
            var offense = Open(new DefenseCombatant());
            var parry = new GestureCommand(1, 0, 1, SwipeDirection.Left, GestureIntent.Parry,
                new InteractionPhase(1, InteractionPhaseKind.EnemySequence), Point(), Point());

            Assert.That(offense.RequestAttack(parry, 0).Status, Is.EqualTo(AttackRequestStatus.Rejected));
            Assert.That(offense.RequestAttack(Command(SwipeDirection.Left, phaseId: 3), 0).Status,
                Is.EqualTo(AttackRequestStatus.Rejected));
            Assert.That(offense.HasBuffer, Is.False);
            Assert.That(offense.ActiveAttack.HasValue, Is.False);
        }

        [Test]
        public void OneBufferKeepsTheFirstDirectionUntilSharedRecoveryCompletes()
        {
            var offense = Open(new DefenseCombatant());
            var first = offense.RequestAttack(Command(SwipeDirection.Left), 0).Attack.Value;
            offense.ResolveImpact(first.Id, 100000);
            var buffered = offense.RequestAttack(Command(SwipeDirection.Right, 400000, sequence: 2), 400000);

            Assert.That(buffered.Status, Is.EqualTo(AttackRequestStatus.Buffered));
            Assert.That(buffered.Attack.HasValue, Is.False);
            Assert.That(offense.BufferedExpiresUs, Is.EqualTo(520000));
            Assert.That(offense.RequestAttack(Command(SwipeDirection.Up, 450000, sequence: 3), 450000).Status,
                Is.EqualTo(AttackRequestStatus.Rejected));
            offense.CompleteRecovery(first.Id, 500000);
            var consumed = offense.TryConsumeBuffer(500000);

            Assert.That(consumed.Status, Is.EqualTo(AttackRequestStatus.Started));
            Assert.That(consumed.Attack.Value.Direction, Is.EqualTo(SwipeDirection.Right));
            Assert.That(consumed.Attack.Value.StartedUs, Is.EqualTo(500000));
            Assert.That(consumed.Attack.Value.Phase.Id, Is.EqualTo(2));
            Assert.That(offense.HasBuffer, Is.False);
        }

        [Test]
        public void ReadyInputCannotOvertakeAnUnexpiredBufferedCommand()
        {
            var offense = Open(new DefenseCombatant());
            var first = offense.RequestAttack(Command(SwipeDirection.Left), 0).Attack.Value;
            offense.ResolveImpact(first.Id, 100000);
            offense.RequestAttack(Command(SwipeDirection.Right, 400000), 400000);

            Assert.That(offense.RequestAttack(Command(SwipeDirection.Up, 500000), 500000).Status,
                Is.EqualTo(AttackRequestStatus.Rejected));
            Assert.That(offense.TryConsumeBuffer(500000).Attack.Value.Direction, Is.EqualTo(SwipeDirection.Right));
        }

        [Test]
        public void ExpiredBufferDoesNotBlockAReadyFreshInput()
        {
            var offense = Open(new DefenseCombatant());
            offense.RequestAttack(Command(SwipeDirection.Left), 0);
            offense.RequestAttack(Command(SwipeDirection.Right, 380000), 380000);

            var fresh = offense.RequestAttack(Command(SwipeDirection.Up, 500000), 500000);

            Assert.That(fresh.Status, Is.EqualTo(AttackRequestStatus.Started));
            Assert.That(fresh.Attack.Value.Direction, Is.EqualTo(SwipeDirection.Up));
            Assert.That(offense.HasBuffer, Is.False);
        }

        [Test]
        public void EarlyConsumptionAttemptPreservesTheUnexpiredFirstCommand()
        {
            var offense = Open(new DefenseCombatant());
            var first = offense.RequestAttack(Command(SwipeDirection.Left), 0).Attack.Value;
            offense.ResolveImpact(first.Id, 100000);
            offense.RequestAttack(Command(SwipeDirection.Right, 400000), 400000);

            Assert.That(offense.TryConsumeBuffer(400001).Status, Is.EqualTo(AttackRequestStatus.Rejected));
            Assert.That(offense.HasBuffer, Is.True);
            Assert.That(offense.TryConsumeBuffer(500000).Attack.Value.Direction, Is.EqualTo(SwipeDirection.Right));
        }

        [TestCase(119999, AttackRequestStatus.Started)]
        [TestCase(120000, AttackRequestStatus.Rejected)]
        [TestCase(120001, AttackRequestStatus.Rejected)]
        public void BufferExpiresAtTheExactAuthoredDeadline(long elapsedUs, AttackRequestStatus status)
        {
            var player = new DefenseCombatant();
            player.ApplyControl(DefenseRulesTests.Control(DefenseCommandKind.DodgeLeft), 0);
            var offense = Open(player);
            offense.RequestAttack(Command(SwipeDirection.Up, 360000 - elapsedUs), 360000 - elapsedUs);

            var consumed = offense.TryConsumeBuffer(360000);

            Assert.That(consumed.Status, Is.EqualTo(status));
            Assert.That(offense.HasBuffer, Is.False);
        }

        [Test]
        public void StaleCommandWhileBusyCannotOccupyTheBuffer()
        {
            var offense = Open(new DefenseCombatant());
            offense.RequestAttack(Command(SwipeDirection.Left), 0);

            Assert.That(offense.RequestAttack(Command(SwipeDirection.Right, 1, phaseId: 1), 1).Status,
                Is.EqualTo(AttackRequestStatus.Rejected));
            Assert.That(offense.HasBuffer, Is.False);
        }

        [Test]
        public void ReopeningCannotConsumeABufferCapturedInThePreviousOpening()
        {
            var offense = Open(new DefenseCombatant());
            offense.RequestAttack(Command(SwipeDirection.Left), 0);
            offense.RequestAttack(Command(SwipeDirection.Right, 400000), 400000);
            offense.CloseOpening(400001);
            offense.StartOpening(Phase(4), 400001, 2000000);

            Assert.That(offense.TryConsumeBuffer(500000).Status, Is.EqualTo(AttackRequestStatus.Rejected));
            Assert.That(offense.HasBuffer, Is.False);
            Assert.That(offense.EnemyHealth, Is.EqualTo(100));
        }

        [Test]
        public void BufferConsumptionStillRequiresRoomForItsFutureImpact()
        {
            var offense = Open(new DefenseCombatant(), durationUs: 600000);
            var attack = offense.RequestAttack(Command(SwipeDirection.Left), 0).Attack.Value;
            offense.ResolveImpact(attack.Id, 100000);
            offense.RequestAttack(Command(SwipeDirection.Right, 400000), 400000);

            Assert.That(offense.TryConsumeBuffer(500000).Status, Is.EqualTo(AttackRequestStatus.Rejected));
            Assert.That(offense.HasBuffer, Is.False);
        }

        [Test]
        public void ImpactRejectsUnknownEarlyAndDuplicateRecords()
        {
            var offense = Open(new DefenseCombatant());
            var attack = offense.RequestAttack(Command(SwipeDirection.Left), 0).Attack.Value;
            Assert.That(offense.ResolveImpact(attack.Id + 1, 1).HasValue, Is.False);
            Assert.That(offense.ResolveImpact(attack.Id, 99999).HasValue, Is.False);

            var resolution = offense.ResolveImpact(attack.Id, 100000);

            Assert.That(resolution.HasValue, Is.True);
            Assert.That(resolution.Value.Attack.Id, Is.EqualTo(attack.Id));
            Assert.That(resolution.Value.TimeUs, Is.EqualTo(100000));
            Assert.That(resolution.Value.Damage, Is.EqualTo(20));
            Assert.That(resolution.Value.EnemyHealth, Is.EqualTo(80));
            Assert.That(offense.ResolveImpact(attack.Id, 100000).HasValue, Is.False);
            Assert.That(offense.EnemyHealth, Is.EqualTo(80));
        }

        [Test]
        public void ClosingBeforeImpactCancelsDamageButRetainsCommittedRecovery()
        {
            var offense = Open(new DefenseCombatant());
            var attack = offense.RequestAttack(Command(SwipeDirection.Left), 0).Attack.Value;
            offense.CloseOpening(1);

            Assert.That(offense.ResolveImpact(attack.Id, 100000).HasValue, Is.False);
            Assert.That(offense.EnemyHealth, Is.EqualTo(100));
            Assert.That(offense.ActiveAttack.HasValue, Is.False);
            Assert.That(offense.Player.ApplyControl(DefenseRulesTests.Control(DefenseCommandKind.GuardPress), 499999), Is.False);
            offense.CompleteRecovery(attack.Id, 499999);
            Assert.That(offense.ActiveAttack.HasValue, Is.False);
            offense.CompleteRecovery(attack.Id, 500000);
            Assert.That(offense.ActiveAttack.HasValue, Is.False);
            Assert.That(offense.Player.State, Is.EqualTo(PlayerCombatState.Ready));
        }

        [Test]
        public void ANewOpeningNeverMakesAnOldCommittedImpactEligible()
        {
            var offense = Open(new DefenseCombatant());
            var attack = offense.RequestAttack(Command(SwipeDirection.Left), 0).Attack.Value;
            offense.CloseOpening(1);
            offense.StartOpening(Phase(4), 1, 2000000);

            Assert.That(offense.ResolveImpact(attack.Id, 100000).HasValue, Is.False);
            Assert.That(offense.EnemyHealth, Is.EqualTo(100));
        }

        [Test]
        public void PlayerDeathClearsPendingOffenseAndPreventsEnemyDamage()
        {
            var player = new DefenseCombatant();
            var offense = Open(player);
            var attack = offense.RequestAttack(Command(SwipeDirection.Left), 0).Attack.Value;
            offense.RequestAttack(Command(SwipeDirection.Right, 1), 1);
            player.ResolveImpact(DefenseRulesTests.Strike(damage: 100), 2);

            Assert.That(offense.ResolveImpact(attack.Id, 100000).HasValue, Is.False);
            Assert.That(offense.HasBuffer, Is.False);
            Assert.That(offense.EnemyHealth, Is.EqualTo(100));
            Assert.That(offense.RequestAttack(Command(SwipeDirection.Left, 100000), 100000).Status,
                Is.EqualTo(AttackRequestStatus.Rejected));
        }

        [Test]
        public void LethalPlayerDamageSaturatesEnemyHealthAndRejectsFurtherRequests()
        {
            var offense = Open(new DefenseCombatant(), new CombatOffenseTuning(enemyHealth: 10));
            var attack = offense.RequestAttack(Command(SwipeDirection.Left), 0).Attack.Value;

            Assert.That(offense.ResolveImpact(attack.Id, 100000).Value.EnemyHealth, Is.Zero);
            Assert.That(offense.RequestAttack(Command(SwipeDirection.Right, 100001), 100001).Status,
                Is.EqualTo(AttackRequestStatus.Rejected));
            Assert.That(offense.HasBuffer, Is.False);
        }

        [TestCase(SwipeDirection.Left, SwipeDirection.Right, SwipeDirection.Left)]
        [TestCase(SwipeDirection.Right, SwipeDirection.Left, SwipeDirection.Right)]
        [TestCase(SwipeDirection.Up, SwipeDirection.Down, SwipeDirection.Up)]
        [TestCase(SwipeDirection.Down, SwipeDirection.Up, SwipeDirection.Down)]
        public void ThirdAlternatingOppositeHitGainsTenPercentThenResets(SwipeDirection first,
            SwipeDirection second, SwipeDirection third)
        {
            var offense = Open(new DefenseCombatant(), new CombatOffenseTuning(enemyHealth: 200));
            Hit(offense, first, 0);
            Hit(offense, second, 500000);

            var finisher = Hit(offense, third, 1000000);
            var after = Hit(offense, second, 1500000);

            Assert.That(finisher.Attack.ComboFinisher, Is.True);
            Assert.That(finisher.Damage, Is.EqualTo(22));
            Assert.That(finisher.EnemyHealth, Is.EqualTo(138));
            Assert.That(after.Attack.ComboFinisher, Is.False);
            Assert.That(after.Damage, Is.EqualTo(20));
        }

        [TestCase(SwipeDirection.Left, SwipeDirection.Left, SwipeDirection.Left)]
        [TestCase(SwipeDirection.Left, SwipeDirection.Up, SwipeDirection.Left)]
        [TestCase(SwipeDirection.Up, SwipeDirection.Right, SwipeDirection.Up)]
        public void RepeatedAndPerpendicularDirectionsDoNotEarnACombo(SwipeDirection first,
            SwipeDirection second, SwipeDirection third)
        {
            var offense = Open(new DefenseCombatant());
            Hit(offense, first, 0);
            Hit(offense, second, 500000);

            Assert.That(Hit(offense, third, 1000000).Damage, Is.EqualTo(20));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClosureAndSuspensionResetComboAndBuffer(bool suspend)
        {
            var offense = Open(new DefenseCombatant(), new CombatOffenseTuning(enemyHealth: 200));
            Hit(offense, SwipeDirection.Left, 0);
            Hit(offense, SwipeDirection.Right, 500000);
            offense.RequestAttack(Command(SwipeDirection.Left, 900000), 900000);
            if (suspend) offense.Suspend();
            else
            {
                offense.CloseOpening(900001);
                offense.StartOpening(Phase(4), 900001, 2000000);
            }

            var attack = offense.RequestAttack(Command(SwipeDirection.Left, 1000000,
                phaseId: suspend ? 2 : 4), 1000000).Attack.Value;

            Assert.That(attack.ComboFinisher, Is.False);
            Assert.That(attack.Damage, Is.EqualTo(20));
            Assert.That(offense.HasBuffer, Is.False);
        }

        [Test]
        public void SuspensionRetainsActiveAttackAndItsSharedRecoveryTimer()
        {
            var offense = Open(new DefenseCombatant());
            var attack = offense.RequestAttack(Command(SwipeDirection.Left), 0).Attack.Value;
            offense.RequestAttack(Command(SwipeDirection.Right, 1), 1);

            offense.Suspend();

            Assert.That(offense.ActiveAttack.Value.Id, Is.EqualTo(attack.Id));
            Assert.That(offense.Player.State, Is.EqualTo(PlayerCombatState.Attacking));
            Assert.That(offense.HasBuffer, Is.False);
            Assert.That(offense.Player.ApplyControl(DefenseRulesTests.Control(DefenseCommandKind.DodgeLeft), 499999), Is.False);
        }

        [Test]
        public void AuthoredZeroDamageStillUsesCadenceAndNeverChangesEnemyHealth()
        {
            var offense = Open(new DefenseCombatant(), new CombatOffenseTuning(attackDamage: 0));

            Assert.That(Hit(offense, SwipeDirection.Left, 0).Damage, Is.Zero);
            Assert.That(offense.EnemyHealth, Is.EqualTo(100));
            Assert.That(offense.Player.State, Is.EqualTo(PlayerCombatState.Recovery));
        }

        [Test]
        public void InvalidTuningAndUnrepresentableTotalsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(normalOpeningUs: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(breakOpeningUs: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(bufferUs: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(enemyHealth: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(attackDamage: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(windupUs: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(recoveryUs: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(comboBonusPercent: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(balanceMaximum: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(balanceDecayDelayUs: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(balanceDecayStepUs: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(focusMaximum: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(parryBalance: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(dodgeBalance: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(blockBalance: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(parryFocus: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(dodgeFocus: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatOffenseTuning(blockFocus: -1));
            Assert.Throws<OverflowException>(() => new CombatOffenseTuning(windupUs: long.MaxValue));
            Assert.Throws<OverflowException>(() => new CombatOffenseTuning(attackDamage: int.MaxValue));
        }

        [Test]
        public void OpeningOverflowAndInvalidPhaseDoNotAdvanceOrReplaceState()
        {
            var offense = Open(new DefenseCombatant());

            Assert.Throws<OverflowException>(() => offense.StartOpening(Phase(4), long.MaxValue - 1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => offense.StartOpening(InteractionPhase.Inactive, 1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => offense.StartOpening(Phase(4), 1, 0));
            Assert.That(offense.TimeUs, Is.Zero);
            Assert.That(offense.Player.TimeUs, Is.Zero);
            Assert.That(offense.OpeningPhase.Id, Is.EqualTo(2));
            Assert.That(offense.OpeningEndUs, Is.EqualTo(2000000));
        }

        [Test]
        public void AttackOverflowDoesNotAdvancePlayerOrConsumeAnIdentity()
        {
            var offense = new OffenseCombatant(new DefenseCombatant());
            offense.StartOpening(Phase(), long.MaxValue - 200000, 200000);

            Assert.Throws<OverflowException>(() => offense.RequestAttack(
                Command(SwipeDirection.Left, long.MaxValue - 150000), long.MaxValue - 150000));

            Assert.That(offense.TimeUs, Is.EqualTo(long.MaxValue - 200000));
            Assert.That(offense.Player.TimeUs, Is.EqualTo(long.MaxValue - 200000));
            Assert.That(offense.Player.State, Is.EqualTo(PlayerCombatState.Ready));
            Assert.That(offense.ActiveAttack.HasValue, Is.False);
        }

        [Test]
        public void InvalidOrRewoundOperationsPreserveCommittedAttack()
        {
            var offense = Open(new DefenseCombatant());
            var attack = offense.RequestAttack(Command(SwipeDirection.Left, 10), 10).Attack.Value;

            Assert.Throws<ArgumentOutOfRangeException>(() => offense.AdvanceTo(9));
            Assert.Throws<ArgumentOutOfRangeException>(() => offense.CloseOpening(9));
            Assert.Throws<ArgumentOutOfRangeException>(() => offense.RequestAttack(default, 10));
            Assert.That(offense.TimeUs, Is.EqualTo(10));
            Assert.That(offense.ActiveAttack.Value.Id, Is.EqualTo(attack.Id));
            Assert.That(offense.IsOpening, Is.True);
        }

        private static OffenseCombatant Open(DefenseCombatant player, CombatOffenseTuning tuning = null,
            long durationUs = 2000000)
        {
            var offense = new OffenseCombatant(player, tuning);
            offense.StartOpening(Phase(), 0, durationUs);
            return offense;
        }

        private static PlayerAttackResolution Hit(OffenseCombatant offense, SwipeDirection direction, long timeUs)
        {
            var attack = offense.RequestAttack(Command(direction, timeUs, phaseId: offense.OpeningPhase.Id), timeUs).Attack.Value;
            return offense.ResolveImpact(attack.Id, timeUs + 100000).Value;
        }

        private static InteractionPhase Phase(long id = 2)
            => new InteractionPhase(id, InteractionPhaseKind.PlayerOpening);
        private static NormalizedPoint Point() => new NormalizedPoint(0.5, 0.5);
        private static GestureCommand Command(SwipeDirection direction, long timeUs = 0,
            long phaseId = 2, long sequence = 1)
            => new GestureCommand(sequence, timeUs, 1, direction, GestureIntent.Attack, Phase(phaseId), Point(), Point());
    }
}
