using System;
using System.Collections.Generic;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Application.Combat
{
    public sealed class CombatEncounter : IDisposable
    {
        private readonly Dictionary<long, EnemyStrike> strikes = new Dictionary<long, EnemyStrike>();
        private readonly HashSet<long> ownedMilestones = new HashSet<long>();
        private InteractionPhase phase;
        private bool disposed;
        public CombatSession Session { get; }
        public DefenseCombatant Player { get; }
        public InteractionPhase CurrentPhase => phase;
        public bool HasCommittedStrike => strikes.Count > 0;
        public event Action<DefenseResolution> StrikeResolved;
        public event Action<EnemyStrike> TelegraphStarted;
        public event Action<DefenseCommand, long> DefenseControlAccepted;
        public event Action<GestureCommand, long> ParryAccepted;

        public CombatEncounter(CombatSession session, InteractionPhase encounterPhase,
            CombatDefenseTuning tuning = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (encounterPhase.Kind != InteractionPhaseKind.EnemySequence || encounterPhase.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(encounterPhase));
            Session = session;
            phase = encounterPhase;
            Player = new DefenseCombatant(tuning);
            Session.Timeline.Resolved += Resolve;
            Session.FrameAdvanced += Player.AdvanceTo;
            Session.Suspended += Player.ReleaseHeldDefense;
            Session.GuardReleaseRejected += ReleaseRejectedGuard;
        }

        public bool CommitStrike(EnemyStrike strike, long telegraphTimeUs)
            => CommitSequence(new[] { new ScheduledEnemyStrike(strike, telegraphTimeUs) });

        public bool CommitSequence(IReadOnlyList<ScheduledEnemyStrike> schedule,
            CombatMilestone? continuation = null)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CombatEncounter));
            var records = EnemySequenceAdmission.CreateBatch(schedule, continuation);
            if (phase.Kind != InteractionPhaseKind.EnemySequence ||
                Player.State == PlayerCombatState.Dead) return false;
            foreach (var step in schedule)
                if (strikes.ContainsKey(step.Strike.Id)) return false;
            if (!Session.Timeline.TryScheduleBatch(records)) return false;
            // The continuation belongs to the caller, never to the defense encounter.
            for (int i = 0; i < schedule.Count * 3; i++) ownedMilestones.Add(records[i].Id);
            foreach (var step in schedule) strikes.Add(step.Strike.Id, step.Strike);
            return true;
        }

        private void Resolve(CombatTimelineEvent record)
        {
            Player.AdvanceTo(record.TimeUs);
            if (record.DefenseCommand.HasValue && (phase.Kind == InteractionPhaseKind.EnemySequence ||
                record.DefenseCommand.Value.Kind == DefenseCommandKind.GuardRelease))
            {
                if (Player.ApplyControl(record.DefenseCommand.Value, record.TimeUs))
                    DefenseControlAccepted?.Invoke(record.DefenseCommand.Value, record.TimeUs);
            }
            else if (record.Command.HasValue)
                InterpretGesture(record.Command.Value, record.TimeUs);
            else if (record.Milestone.HasValue)
                ResolveMilestone(record.Milestone.Value);
        }

        public void SetPhase(InteractionPhase next)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CombatEncounter));
            if (next.Id <= phase.Id) throw new ArgumentOutOfRangeException(nameof(next));
            phase = next;
            Player.ReleaseHeldDefense();
        }

        public void InterpretGesture(GestureCommand command, long timeUs)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CombatEncounter));
            if (phase.Kind != InteractionPhaseKind.EnemySequence ||
                command.Intent != GestureIntent.Parry || command.Phase.Id != phase.Id ||
                command.Phase.Kind != phase.Kind ||
                !Session.Timeline.TryGetNextImpact(out var incoming)) return;
            var id = (incoming.Id - 1) / 3 + 1;
            if (!ownedMilestones.Contains(incoming.Id)) return;
            if (strikes.TryGetValue(id, out var strike))
            {
                if (Player.TryParry(strike, incoming.TimeUs, command.Direction, timeUs))
                    ParryAccepted?.Invoke(command, timeUs);
            }
        }

        private void ResolveMilestone(CombatMilestone milestone)
        {
            if (!ownedMilestones.Remove(milestone.Id)) return;
            var id = (milestone.Id - 1) / 3 + 1;
            if (!strikes.TryGetValue(id, out var strike)) return;
            switch (milestone.Kind)
            {
                case CombatMilestoneKind.Telegraph:
                    if (Player.State != PlayerCombatState.Dead) TelegraphStarted?.Invoke(strike);
                    break;
                case CombatMilestoneKind.Impact:
                    var result = Player.ResolveImpact(strike, milestone.TimeUs);
                    if (result.Outcome != DefenseOutcome.IgnoredAfterDeath) StrikeResolved?.Invoke(result);
                    break;
                case CombatMilestoneKind.RecoveryComplete:
                    strikes.Remove(id);
                    break;
            }
        }

        private void ReleaseRejectedGuard(DefenseCommand command) => Player.ApplyControl(command, Player.TimeUs);

        public void CancelPendingStrikes()
        {
            foreach (var id in ownedMilestones) Session.Timeline.TryCancelMilestone(id);
            ownedMilestones.Clear();
            strikes.Clear();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Session.Timeline.Resolved -= Resolve;
            Session.FrameAdvanced -= Player.AdvanceTo;
            Session.Suspended -= Player.ReleaseHeldDefense;
            Session.GuardReleaseRejected -= ReleaseRejectedGuard;
            Player.ReleaseHeldDefense();
            CancelPendingStrikes();
        }
    }
}
