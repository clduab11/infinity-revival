using System;
using Praxen.Game.Application.Combat;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Presentation.Combat
{
    /// <summary>Copies accepted actions and resolved outcomes. Never mutates combat.</summary>
    public sealed class CombatPresentationObserver : IDisposable
    {
        private readonly CombatEncounter encounter;
        private readonly CombatOpeningController duel;
        private EnemyStrike incoming;
        private long tellStarted, enemyImpact, enemyRecovery;
        private long phaseId;
        private bool disposed;
        public ActorMotion PlayerMotion { get; private set; } = Idle(0);
        public ActorMotion EnemyMotion { get; private set; } = Idle(0);
        public event Action<CombatPresentationCue> Cue;

        public CombatPresentationObserver(CombatEncounter encounter, CombatOpeningController duel)
        {
            this.encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            this.duel = duel ?? throw new ArgumentNullException(nameof(duel));
            phaseId = duel.CurrentPhase.Id;
            encounter.DefenseControlAccepted += Defense;
            encounter.ParryAccepted += Parry;
            encounter.TelegraphStarted += Tell;
            encounter.StrikeResolved += EnemyImpact;
            duel.PlayerAttackStarted += Attack;
            duel.PlayerStrikeResolved += PlayerImpact;
            encounter.Session.IncomingWarningRearmed += Rearm;
        }

        public void Sample(long timeUs)
        {
            if (disposed) return;
            if (PlayerMotion.Key != "Death" && !PlayerMotion.Loop && timeUs >= PlayerMotion.EndUs)
                PlayerMotion = Rest(PlayerMotion.EndUs);
            if (PlayerMotion.Key == "Guard" && !encounter.Player.GuardHeld)
                PlayerMotion = Idle(timeUs);
            if (EnemyMotion.Key == "Hit" && timeUs >= EnemyMotion.EndUs) EnemyMotion = Idle(EnemyMotion.EndUs);
            if (incoming != null && EnemyMotion.Key != "Hit" && EnemyMotion.Key != "Death")
                SampleIncoming(timeUs);
            if (phaseId != duel.CurrentPhase.Id)
            {
                phaseId = duel.CurrentPhase.Id;
                if (duel.CurrentPhase.Kind == InteractionPhaseKind.PlayerOpening)
                    Emit(CombatCueKind.Opening, duel.Offense.OpeningStartUs);
            }
        }

        private void Defense(DefenseCommand command, long time)
        {
            if (command.Kind == DefenseCommandKind.DodgeLeft || command.Kind == DefenseCommandKind.DodgeRight)
            {
                PlayerMotion = new ActorMotion(command.Kind == DefenseCommandKind.DodgeLeft ? "DodgeLeft" : "DodgeRight",
                    time, encounter.Player.ActionEndsUs);
                Emit(CombatCueKind.Dodge, time);
            }
            else if (encounter.Player.State == PlayerCombatState.Guarding)
            {
                PlayerMotion = new ActorMotion("Guard", time, loop: true);
                Emit(CombatCueKind.Guard, time);
            }
            else if (PlayerMotion.Key == "Guard") PlayerMotion = Idle(time);
        }

        private void Parry(GestureCommand command, long time) => PlayerMotion =
            new ActorMotion("Parry", time, encounter.Player.ActionEndsUs + encounter.Player.Tuning.ParryRecoveryUs);

        private void Attack(PlayerAttack attack)
        {
            PlayerMotion = new ActorMotion("Cut" + attack.Direction, attack.StartedUs, attack.RecoveryEndUs, attack.ImpactUs);
            Emit(CombatCueKind.Attack, attack.StartedUs);
        }

        private void Tell(EnemyStrike strike)
        {
            incoming = strike;
            tellStarted = encounter.Session.Timeline.TimeUs;
            RefreshIncoming();
            SampleIncoming(tellStarted);
            Emit(CombatCueKind.Telegraph, tellStarted, true);
        }

        private void RefreshIncoming()
        {
            if (incoming == null) return;
            var timeline = encounter.Session.Timeline;
            if (timeline.TryGetMilestone(checked(incoming.Id * 3 - 1), out var impact)) enemyImpact = impact.TimeUs;
            if (timeline.TryGetMilestone(checked(incoming.Id * 3), out var recovery)) enemyRecovery = recovery.TimeUs;
        }

        private void SampleIncoming(long time)
        {
            RefreshIncoming();
            long attackStart = Math.Max(tellStarted, enemyImpact - 100000);
            EnemyMotion = time >= enemyRecovery ? Idle(enemyRecovery) : time < attackStart
                ? new ActorMotion("EnemyTell", tellStarted, attackStart)
                : new ActorMotion("EnemyAttack", attackStart, enemyRecovery, enemyImpact);
            if (time >= enemyRecovery) incoming = null;
        }

        private void Rearm(long impact)
        {
            if (incoming == null || EnemyMotion.Key == "Death") return;
            tellStarted = encounter.Session.Clock.TimeUs;
            RefreshIncoming();
            SampleIncoming(tellStarted);
            Emit(CombatCueKind.Telegraph, tellStarted, true);
        }

        private void EnemyImpact(DefenseResolution result)
        {
            if (result.IsDead)
            {
                PlayerMotion = new ActorMotion("Death", result.TimeUs, result.TimeUs + 1000000);
                Emit(CombatCueKind.Death, result.TimeUs);
            }
            else if (result.Outcome == DefenseOutcome.Hit || result.Outcome == DefenseOutcome.GuardBroken)
            {
                PlayerMotion = new ActorMotion("Hit", result.TimeUs, result.TimeUs + 300000);
                Emit(CombatCueKind.Hit, result.TimeUs);
            }
            else if (result.Outcome == DefenseOutcome.Parried) Emit(CombatCueKind.Parry, result.TimeUs);
            else if (result.Outcome == DefenseOutcome.Blocked)
            {
                PlayerMotion = new ActorMotion("Hit", result.TimeUs, result.TimeUs + 180000);
                Emit(CombatCueKind.Block, result.TimeUs);
            }
        }

        private void PlayerImpact(PlayerAttackResolution result)
        {
            EnemyMotion = new ActorMotion(result.EnemyHealth == 0 ? "Death" : "Hit", result.TimeUs,
                result.TimeUs + (result.EnemyHealth == 0 ? 1000000 : 300000));
            Emit(result.EnemyHealth == 0 ? CombatCueKind.Death : CombatCueKind.Hit, result.TimeUs, true);
        }

        private ActorMotion Rest(long time) => encounter.Player.GuardHeld
            ? new ActorMotion("Guard", time, loop: true) : Idle(time);
        private static ActorMotion Idle(long time) => new ActorMotion("Idle", time, loop: true);
        private void Emit(CombatCueKind kind, long time, bool enemy = false) =>
            Cue?.Invoke(new CombatPresentationCue(kind, time, enemy));

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            encounter.DefenseControlAccepted -= Defense;
            encounter.ParryAccepted -= Parry;
            encounter.TelegraphStarted -= Tell;
            encounter.StrikeResolved -= EnemyImpact;
            duel.PlayerAttackStarted -= Attack;
            duel.PlayerStrikeResolved -= PlayerImpact;
            encounter.Session.IncomingWarningRearmed -= Rearm;
            Cue = null;
        }
    }
}
