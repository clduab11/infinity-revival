using System;
using System.Collections.Generic;
using Praxen.Game.Application.Input;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Application.Combat
{
    public enum DuelOutcome { Running, Victory, Defeat }

    /// <summary>Coordinates one enemy sequence and its half-open player opening.</summary>
    public sealed class CombatOpeningController : IDisposable
    {
        private enum RecordKind { OpeningStart, OpeningEnd, PlayerImpact, Recovery, Buffered }

        private readonly struct OwnedRecord
        {
            internal readonly RecordKind Kind;
            internal readonly long AttackId;
            internal OwnedRecord(RecordKind kind, long attackId = 0)
            {
                Kind = kind;
                AttackId = attackId;
            }
        }

        private readonly GesturePhaseContext phases;
        private readonly CardinalGestureRecognizer recognizer;
        private readonly Dictionary<long, OwnedRecord> records = new Dictionary<long, OwnedRecord>();
        private readonly CombatOffenseTuning tuning;
        private long openingStartId;
        private long openingEndId;
        private long openingImpactId;
        private bool awaitingEnemyAdmission;
        private bool retryEnemyAdmission;
        private bool disposed;

        public CombatEncounter Encounter { get; }
        public OffenseCombatant Offense { get; }
        public CombatMomentum Momentum { get; }
        public InteractionPhase CurrentPhase => Encounter.CurrentPhase;
        public DuelOutcome Outcome { get; private set; }
        public long OpeningRemainingUs => Outcome == DuelOutcome.Running &&
            CurrentPhase.Kind == InteractionPhaseKind.PlayerOpening ?
            Math.Max(0, Offense.OpeningEndUs - Encounter.Session.Clock.TimeUs) : 0;

        public event Action<long> EnemySequenceReady;
        public event Action<PlayerAttackResolution> PlayerStrikeResolved;
        public event Action<PlayerAttack> PlayerAttackStarted;
        public event Action<GestureCommand> GestureRecognized;

        public CombatOpeningController(CombatEncounter encounter, GesturePhaseContext phases,
            CombatOffenseTuning tuning = null, GestureTuning gestureTuning = null)
        {
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            this.phases = phases ?? throw new ArgumentNullException(nameof(phases));
            this.tuning = tuning ?? new CombatOffenseTuning();
            if (phases.Current.Id < encounter.CurrentPhase.Id)
                phases.SetPhase(encounter.CurrentPhase);
            if (phases.Current.Id != encounter.CurrentPhase.Id ||
                phases.Current.Kind != encounter.CurrentPhase.Kind)
                throw new ArgumentException("The phase ports must describe the same encounter phase.", nameof(phases));
            Offense = new OffenseCombatant(encounter.Player, this.tuning);
            Momentum = new CombatMomentum(this.tuning);
            recognizer = new CardinalGestureRecognizer(gestureTuning);
            encounter.Session.Timeline.BeforeResolve += BeforeResolve;
            encounter.Session.Timeline.Resolved += Resolve;
            encounter.Session.FrameAdvanced += AdvanceFrame;
            encounter.Session.Suspended += Suspend;
            encounter.Session.TouchInputRejected += CancelContacts;
            encounter.StrikeResolved += RewardDefense;
        }

        public bool CommitStrike(EnemyStrike strike, long telegraphTimeUs)
            => CommitSequence(new[] { new ScheduledEnemyStrike(strike, telegraphTimeUs) });

        public bool CommitSequence(IReadOnlyList<ScheduledEnemyStrike> schedule)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CombatOpeningController));
            var enemyRecords = EnemySequenceAdmission.CreateBatch(schedule);
            if (Outcome != DuelOutcome.Running || CurrentPhase.Kind != InteractionPhaseKind.EnemySequence ||
                Encounter.HasCommittedStrike || openingStartId != 0) return false;
            var last = schedule[schedule.Count - 1];
            long recoveryUs = last.RecoveryTimeUs;
            _ = checked(recoveryUs + Math.Max(tuning.NormalOpeningUs, tuning.BreakOpeningUs));
            var timeline = Encounter.Session.Timeline;
            if (timeline.AvailableCapacity < enemyRecords.Length + 1)
            {
                retryEnemyAdmission = awaitingEnemyAdmission;
                return false;
            }
            long id = EnemySequenceAdmission.AllocateBoundary(timeline, enemyRecords);
            if (!Encounter.CommitSequence(schedule,
                new CombatMilestone(id, recoveryUs, CombatMilestoneKind.PhaseTransition))) return false;
            records.Add(id, new OwnedRecord(RecordKind.OpeningStart));
            openingStartId = id;
            openingImpactId = checked(last.Strike.Id * 3) - 1;
            awaitingEnemyAdmission = false;
            retryEnemyAdmission = false;
            return true;
        }

        private void BeforeResolve(CombatTimelineEvent record)
        {
            if (disposed || Outcome != DuelOutcome.Running) return;
            AdvanceResources(record.TimeUs);
            if (FinishIfTerminal()) return;
            SynchronizePhase(record);
        }

        private void SynchronizePhase(CombatTimelineEvent record, bool afterEnemyImpact = false)
        {
            if (openingStartId != 0 && TryGetBoundary(openingStartId, record, out var start) &&
                record.TimeUs >= start.TimeUs &&
                (afterEnemyImpact || !EnemyImpactMustResolveFirst(record, start.TimeUs)))
                StartOpening(start.TimeUs);
            if (openingEndId != 0 && TryGetBoundary(openingEndId, record, out var end) &&
                record.TimeUs >= end.TimeUs)
                CloseOpening(end.TimeUs);
        }

        private bool EnemyImpactMustResolveFirst(CombatTimelineEvent record, long startUs)
        {
            if (record.Milestone.HasValue && record.Milestone.Value.Id == openingImpactId &&
                record.TimeUs == startUs) return true;
            return Encounter.Session.Timeline.TryGetMilestone(openingImpactId, out var impact) &&
                impact.TimeUs <= startUs;
        }

        private bool TryGetBoundary(long id, CombatTimelineEvent record, out CombatMilestone milestone)
        {
            if (record.Milestone.HasValue && record.Milestone.Value.Id == id)
            {
                milestone = record.Milestone.Value;
                return true;
            }
            return Encounter.Session.Timeline.TryGetMilestone(id, out milestone);
        }

        private void StartOpening(long timeUs)
        {
            if (Outcome != DuelOutcome.Running || openingStartId == 0) return;
            bool balanceBreak = Momentum.PendingBalanceBreak;
            long durationUs = balanceBreak ? tuning.BreakOpeningUs : tuning.NormalOpeningUs;
            long endUs = checked(timeUs + durationUs);
            var phase = NextPhase(InteractionPhaseKind.PlayerOpening);
            long id = Encounter.Session.Timeline.AllocateMilestoneId();
            if (!Encounter.Session.Timeline.TrySchedule(
                new CombatMilestone(id, endUs, CombatMilestoneKind.PhaseTransition)))
                throw new InvalidOperationException("The committed opening could not reserve its closure.");
            records.Add(id, new OwnedRecord(RecordKind.OpeningEnd));
            openingEndId = id;
            openingStartId = 0;
            openingImpactId = 0;
            if (balanceBreak) Momentum.ConsumeBalanceBreak();
            Offense.StartOpening(phase, timeUs, durationUs);
            SetPhase(phase);
        }

        private void CloseOpening(long timeUs)
        {
            if (Outcome != DuelOutcome.Running || openingEndId == 0) return;
            Offense.CloseOpening(timeUs);
            openingEndId = 0;
            CancelRecords();
            SetPhase(NextPhase(InteractionPhaseKind.EnemySequence));
            awaitingEnemyAdmission = true;
            retryEnemyAdmission = false;
            EnemySequenceReady?.Invoke(timeUs);
        }

        private void Resolve(CombatTimelineEvent record)
        {
            if (disposed || Outcome != DuelOutcome.Running) return;
            if (FinishIfTerminal()) return;
            if (record.Milestone.HasValue && record.Milestone.Value.Id == openingImpactId)
                SynchronizePhase(record, true);
            if (record.Touch.HasValue)
            {
                var touch = record.Touch.Value;
                var command = recognizer.Process(touch.Sample, touch.Owner, CurrentPhase, touch.Metrics);
                if (command.HasValue)
                {
                    ApplyGesture(command.Value, record.TimeUs);
                    GestureRecognized?.Invoke(command.Value);
                }
            }
            else if (record.Command.HasValue && record.Command.Value.Intent == GestureIntent.Attack)
                ApplyGesture(record.Command.Value, record.TimeUs);
            else if (record.Milestone.HasValue)
                ResolveMilestone(record.Milestone.Value);
            FinishIfTerminal();
        }

        private void ApplyGesture(GestureCommand command, long timeUs)
        {
            if (Outcome != DuelOutcome.Running || command.Phase.Id != CurrentPhase.Id ||
                command.Phase.Kind != CurrentPhase.Kind) return;
            if (command.Intent == GestureIntent.Parry)
                Encounter.InterpretGesture(command, timeUs);
            else if (CurrentPhase.Kind == InteractionPhaseKind.PlayerOpening)
                RequestAttack(command, timeUs);
        }

        private void RequestAttack(GestureCommand command, long timeUs)
        {
            if (Encounter.Player.State != PlayerCombatState.Ready &&
                Encounter.Player.State != PlayerCombatState.Guarding)
            {
                BufferDuringRecovery(command, timeUs);
                return;
            }
            AdmitAttack(timeUs, () => Offense.RequestAttack(command, timeUs));
        }

        private void BufferDuringRecovery(GestureCommand command, long timeUs)
        {
            if (Offense.ActiveAttack.HasValue || Offense.HasBuffer || HasRecoveryBufferTrigger())
            {
                Offense.RequestAttack(command, timeUs);
                return;
            }
            var timeline = Encounter.Session.Timeline;
            long recoveryUs = Encounter.Player.ActionEndsUs;
            if (recoveryUs <= timeUs || timeline.AvailableCapacity == 0) return;
            long id = timeline.AllocateMilestoneId();
            if (!timeline.TrySchedule(new CombatMilestone(id, recoveryUs, CombatMilestoneKind.BufferedOffense)))
                return;
            AttackRequestResult result;
            try { result = Offense.RequestAttack(command, timeUs); }
            catch
            {
                timeline.TryCancelMilestone(id);
                throw;
            }
            if (result.Status == AttackRequestStatus.Buffered)
                records.Add(id, new OwnedRecord(RecordKind.Buffered));
            else
                timeline.TryCancelMilestone(id);
        }

        private bool HasRecoveryBufferTrigger()
        {
            foreach (var record in records.Values)
                if (record.Kind == RecordKind.Buffered && record.AttackId == 0) return true;
            return false;
        }

        private void AdmitAttack(long timeUs, Func<AttackRequestResult> request)
        {
            var timeline = Encounter.Session.Timeline;
            if (timeline.AvailableCapacity < 3) return;
            long impactUs = checked(timeUs + tuning.WindupUs);
            long recoveryUs = checked(impactUs + tuning.RecoveryUs);
            if (CurrentPhase.Kind != InteractionPhaseKind.PlayerOpening || impactUs >= Offense.OpeningEndUs)
                return;
            long impactId = timeline.AllocateMilestoneId();
            long recoveryId = timeline.AllocateMilestoneId();
            long bufferedId = timeline.AllocateMilestoneId();
            var batch = new[] {
                new CombatMilestone(impactId, impactUs, CombatMilestoneKind.PlayerImpact),
                new CombatMilestone(recoveryId, recoveryUs, CombatMilestoneKind.RecoveryComplete),
                new CombatMilestone(bufferedId, recoveryUs, CombatMilestoneKind.BufferedOffense) };
            if (!timeline.TryScheduleBatch(batch)) return;
            AttackRequestResult result;
            try { result = request(); }
            catch
            {
                foreach (var milestone in batch) timeline.TryCancelMilestone(milestone.Id);
                throw;
            }
            if (result.Status != AttackRequestStatus.Started || !result.Attack.HasValue)
            {
                foreach (var milestone in batch) timeline.TryCancelMilestone(milestone.Id);
                return;
            }
            long attackId = result.Attack.Value.Id;
            records.Add(impactId, new OwnedRecord(RecordKind.PlayerImpact, attackId));
            records.Add(recoveryId, new OwnedRecord(RecordKind.Recovery, attackId));
            records.Add(bufferedId, new OwnedRecord(RecordKind.Buffered, attackId));
            PlayerAttackStarted?.Invoke(result.Attack.Value);
        }

        private void ResolveMilestone(CombatMilestone milestone)
        {
            if (!records.TryGetValue(milestone.Id, out var owned)) return;
            records.Remove(milestone.Id);
            switch (owned.Kind)
            {
                case RecordKind.PlayerImpact:
                    var result = Offense.ResolveImpact(owned.AttackId, milestone.TimeUs);
                    if (result.HasValue) PlayerStrikeResolved?.Invoke(result.Value);
                    break;
                case RecordKind.Recovery:
                    Offense.CompleteRecovery(owned.AttackId, milestone.TimeUs);
                    break;
                case RecordKind.Buffered:
                    if (Offense.HasBuffer)
                        AdmitAttack(milestone.TimeUs, () => Offense.TryConsumeBuffer(milestone.TimeUs));
                    break;
            }
        }

        private void RewardDefense(DefenseResolution resolution)
        {
            if (!disposed && Outcome == DuelOutcome.Running)
            {
                Momentum.RecordDefense(resolution);
                FinishIfTerminal();
            }
        }

        private void AdvanceFrame(long timeUs)
        {
            if (disposed || Outcome != DuelOutcome.Running) return;
            AdvanceResources(timeUs);
            if (!FinishIfTerminal() && awaitingEnemyAdmission && retryEnemyAdmission &&
                CurrentPhase.Kind == InteractionPhaseKind.EnemySequence)
                EnemySequenceReady?.Invoke(timeUs);
        }

        private void AdvanceResources(long timeUs)
        {
            Momentum.AdvanceTo(timeUs);
            Offense.AdvanceTo(timeUs);
        }

        private bool FinishIfTerminal()
        {
            if (Outcome != DuelOutcome.Running) return true;
            if (Encounter.Player.State != PlayerCombatState.Dead && Offense.EnemyHealth != 0)
                return false;
            Outcome = Encounter.Player.State == PlayerCombatState.Dead ? DuelOutcome.Defeat : DuelOutcome.Victory;
            Stop();
            return true;
        }

        private InteractionPhase NextPhase(InteractionPhaseKind kind) => new InteractionPhase(
            checked(Math.Max(CurrentPhase.Id, phases.Current.Id) + 1), kind);

        private void SetPhase(InteractionPhase phase)
        {
            Encounter.SetPhase(phase);
            phases.SetPhase(phase);
            recognizer.CancelAll();
        }

        private void Suspend()
        {
            if (disposed || Outcome != DuelOutcome.Running) return;
            recognizer.ResetContacts();
            Offense.Suspend();
        }

        public void CancelContacts() => recognizer.ResetContacts();

        private void CancelRecords()
        {
            var canceled = new List<long>(records.Keys);
            records.Clear();
            foreach (long id in canceled)
                Encounter.Session.Timeline.TryCancelMilestone(id);
        }

        private void Stop()
        {
            Offense.CloseOpening(Offense.TimeUs);
            Offense.Suspend();
            Momentum.Stop();
            recognizer.ResetContacts();
            Encounter.CancelPendingStrikes();
            CancelRecords();
            openingStartId = 0;
            openingEndId = 0;
            openingImpactId = 0;
            awaitingEnemyAdmission = false;
            retryEnemyAdmission = false;
            SetPhase(NextPhase(InteractionPhaseKind.Inactive));
        }

        public void Dispose()
        {
            if (disposed) return;
            if (Outcome == DuelOutcome.Running) Stop();
            disposed = true;
            Encounter.Session.Timeline.BeforeResolve -= BeforeResolve;
            Encounter.Session.Timeline.Resolved -= Resolve;
            Encounter.Session.FrameAdvanced -= AdvanceFrame;
            Encounter.Session.Suspended -= Suspend;
            Encounter.Session.TouchInputRejected -= CancelContacts;
            Encounter.StrikeResolved -= RewardDefense;
        }
    }
}
