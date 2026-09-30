using System;
using System.Collections.Generic;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Application.Combat
{
    public enum EnemyDirectorState { Waiting, Prepared, Executing, PlayerOpening, Terminal }

    /// <summary>Commits immutable seeded patterns only at safe enemy admission boundaries.</summary>
    public sealed class EnemyPatternDirector : IDisposable
    {
        private readonly CombatOpeningController duel;
        private readonly CombatSession session;
        private readonly long telegraphLeadUs;
        private EnemyPatternDecision prepared;
        private DefenseOutcome? lastDefenseOutcome;
        private SwipeDirection? lastAttackDirection;
        private long nextStrikeId = 1;
        private long wakeId;
        private long wakeTimeUs;
        private long wakeArrivalOrder;
        private bool started;
        private bool terminal;
        private bool disposed;

        public EnemyDirectorState State { get; private set; }
        public EnemyPatternSelector Selector { get; }
        public EnemyPatternDecision CurrentDecision { get; private set; }
        public IReadOnlyList<ScheduledEnemyStrike> CurrentSchedule { get; private set; } =
            Array.Empty<ScheduledEnemyStrike>();
        public string ContentRevision { get; }
        public string BalanceRevision { get; }
        public event Action<EnemyPatternDecision> PatternCommitted;

        public EnemyPatternDirector(CombatOpeningController duel, EnemyPatternDeck deck, uint seed,
            int difficultyTier = 0, long telegraphLeadUs = 10000)
        {
            this.duel = duel ?? throw new ArgumentNullException(nameof(duel));
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            if (telegraphLeadUs <= 0) throw new ArgumentOutOfRangeException(nameof(telegraphLeadUs));
            this.telegraphLeadUs = telegraphLeadUs;
            session = duel.Encounter.Session;
            Selector = new EnemyPatternSelector(deck, seed, difficultyTier);
            ContentRevision = deck.ContentRevision;
            BalanceRevision = deck.BalanceRevision;
            State = EnemyDirectorState.Waiting;
            duel.EnemySequenceReady += SequenceReady;
            duel.Encounter.StrikeResolved += DefenseResolved;
            duel.PlayerStrikeResolved += PlayerStrikeResolved;
            session.Timeline.Resolved += TimelineResolved;
            session.FrameAdvanced += FrameAdvanced;
            session.Suspended += Suspended;
            SynchronizeState();
        }

        public bool Start(long timeUs = 0)
        {
            if (disposed) throw new ObjectDisposedException(nameof(EnemyPatternDirector));
            if (timeUs < 0) throw new ArgumentOutOfRangeException(nameof(timeUs));
            if (!SynchronizeState()) return false;
            started = true;
            if (!SafeToAdmit()) return false;
            return TryAdmit(timeUs);
        }

        private bool SafeToAdmit() => !terminal && session.Clock.State == CombatClockState.Running &&
            duel.Outcome == DuelOutcome.Running &&
            duel.CurrentPhase.Kind == InteractionPhaseKind.EnemySequence &&
            !duel.Encounter.HasCommittedStrike;

        private bool TryAdmit(long timeUs)
        {
            if (!SafeToAdmit()) return false;
            if (prepared == null)
            {
                var player = duel.Encounter.Player;
                var observation = new EnemyObservation(player.Health, player.Guard, player.DodgeCharges,
                    duel.Momentum.Focus, lastDefenseOutcome, lastAttackDirection);
                if (!Selector.TryPrepare(timeUs, observation, out prepared, out var nextEligibleTimeUs))
                {
                    State = EnemyDirectorState.Waiting;
                    if (nextEligibleTimeUs.HasValue) ScheduleWake(nextEligibleTimeUs.Value, timeUs);
                    else CancelWake();
                    return false;
                }
                CurrentDecision = prepared;
                State = EnemyDirectorState.Prepared;
            }
            CancelWake();
            if (!Selector.CanCommit(prepared, timeUs)) return false;
            var schedule = BuildSchedule(prepared.Pattern, timeUs, out var followingStrikeId);
            if (!duel.CommitSequence(schedule)) return false;
            if (!Selector.TryCommit(prepared, timeUs))
                throw new InvalidOperationException("A preflighted admitted pattern lost selector ownership.");
            nextStrikeId = followingStrikeId;
            CurrentSchedule = schedule;
            CurrentDecision = prepared;
            prepared = null;
            State = EnemyDirectorState.Executing;
            PatternCommitted?.Invoke(CurrentDecision);
            return true;
        }

        private IReadOnlyList<ScheduledEnemyStrike> BuildSchedule(EnemyPattern pattern, long timeUs,
            out long followingStrikeId)
        {
            var schedule = new List<ScheduledEnemyStrike>(pattern.Steps.Count);
            long strikeId = nextStrikeId;
            long nextTelegraphUs = checked(timeUs + telegraphLeadUs);
            foreach (var step in pattern.Steps)
            {
                long telegraphUs = checked(nextTelegraphUs + step.GapBeforeUs);
                var strike = step.CreateStrike(strikeId, duel.CurrentPhase.Id);
                var scheduled = new ScheduledEnemyStrike(strike, telegraphUs);
                schedule.Add(scheduled);
                nextTelegraphUs = scheduled.RecoveryTimeUs;
                strikeId = checked(strikeId + 1);
            }
            followingStrikeId = strikeId;
            return schedule.AsReadOnly();
        }

        private void SequenceReady(long timeUs)
        {
            if (!started || !SynchronizeState()) return;
            State = prepared == null ? EnemyDirectorState.Waiting : EnemyDirectorState.Prepared;
            TryAdmit(timeUs);
        }

        private void DefenseResolved(DefenseResolution resolution)
        {
            lastDefenseOutcome = resolution.Outcome;
            SynchronizeState();
        }

        private void PlayerStrikeResolved(PlayerAttackResolution resolution)
        {
            lastAttackDirection = resolution.Attack.Direction;
            SynchronizeState();
        }

        private void TimelineResolved(CombatTimelineEvent record)
        {
            bool ownsWake = record.Milestone.HasValue && record.Milestone.Value.Id == wakeId &&
                record.ArrivalOrder == wakeArrivalOrder;
            if (ownsWake)
            {
                // Release identity before any callback can reuse it for a foreign milestone.
                wakeId = 0;
                wakeTimeUs = 0;
                wakeArrivalOrder = 0;
            }
            if (!SynchronizeState() || !started || !ownsWake) return;
            TryAdmit(record.TimeUs);
        }

        private void FrameAdvanced(long timeUs)
        {
            if (!SynchronizeState() || !started ||
                (State != EnemyDirectorState.Waiting && State != EnemyDirectorState.Prepared)) return;
            TryAdmit(timeUs);
        }

        private void Suspended() => SynchronizeState();

        private bool SynchronizeState()
        {
            if (terminal || disposed) return false;
            if (duel.Outcome != DuelOutcome.Running || duel.Encounter.Player.State == PlayerCombatState.Dead ||
                duel.Offense.EnemyHealth == 0 || duel.CurrentPhase.Kind == InteractionPhaseKind.Inactive)
            {
                Stop();
                return false;
            }
            if (duel.CurrentPhase.Kind == InteractionPhaseKind.PlayerOpening)
                State = EnemyDirectorState.PlayerOpening;
            else if (duel.Encounter.HasCommittedStrike)
                State = EnemyDirectorState.Executing;
            return true;
        }

        private void ScheduleWake(long timeUs, long nowUs)
        {
            if (timeUs <= nowUs) return;
            if (wakeId != 0 && wakeTimeUs == timeUs &&
                session.Timeline.TryGetMilestoneRecord(wakeId, out var existing) &&
                existing.ArrivalOrder == wakeArrivalOrder && existing.TimeUs == wakeTimeUs) return;
            CancelWake();
            if (session.Timeline.AvailableCapacity == 0) return;
            long id = session.Timeline.AllocateMilestoneId();
            if (!session.Timeline.TrySchedule(
                new CombatMilestone(id, timeUs, CombatMilestoneKind.PhaseTransition))) return;
            if (!session.Timeline.TryGetMilestoneRecord(id, out var admitted))
                throw new InvalidOperationException("An admitted cooldown wake lost timeline ownership.");
            wakeId = id;
            wakeTimeUs = timeUs;
            wakeArrivalOrder = admitted.ArrivalOrder;
        }

        private void CancelWake()
        {
            long id = wakeId;
            long arrivalOrder = wakeArrivalOrder;
            wakeId = 0;
            wakeTimeUs = 0;
            wakeArrivalOrder = 0;
            if (id != 0 && session.Timeline.TryGetMilestoneRecord(id, out var record) &&
                record.ArrivalOrder == arrivalOrder)
                session.Timeline.TryCancelMilestone(id);
        }

        private void Stop()
        {
            terminal = true;
            State = EnemyDirectorState.Terminal;
            prepared = null;
            CancelWake();
            duel.EnemySequenceReady -= SequenceReady;
            duel.Encounter.StrikeResolved -= DefenseResolved;
            duel.PlayerStrikeResolved -= PlayerStrikeResolved;
            session.Timeline.Resolved -= TimelineResolved;
            session.FrameAdvanced -= FrameAdvanced;
            session.Suspended -= Suspended;
        }

        public void Dispose()
        {
            if (disposed) return;
            Stop();
            disposed = true;
        }
    }
}
