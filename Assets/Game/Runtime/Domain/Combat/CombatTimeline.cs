using System;
using System.Collections.Generic;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Domain.Combat
{
    public sealed class CombatTimeline
    {
        private readonly int _capacity;
        private readonly List<CombatTimelineEvent> _pending;
        private readonly HashSet<long> _milestoneIds = new HashSet<long>();
        private long _lastArrivalOrder;
        private long _sealedTimeUs;
        private bool _hasSealedTime;
        private bool _advancing;
        private long _nextGeneratedId = long.MaxValue;

        public event Action<CombatTimelineEvent> BeforeResolve;
        public event Action<CombatTimelineEvent> Resolved;
        public long TimeUs { get; private set; }
        public int PendingCount => _pending.Count;
        public int AvailableCapacity => _capacity - _pending.Count;

        public CombatTimeline(int capacity = 1024)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
            _pending = new List<CombatTimelineEvent>(capacity);
        }

        public bool TryEnqueue(GestureCommand command, long combatTimeUs)
        {
            if (combatTimeUs < 0) throw new ArgumentOutOfRangeException(nameof(combatTimeUs));
            ValidateCommand(command);
            if (IsSealed(combatTimeUs) || _pending.Count == _capacity) return false;
            var arrivalOrder = checked(_lastArrivalOrder + 1);
            Insert(new CombatTimelineEvent(command, combatTimeUs, arrivalOrder));
            _lastArrivalOrder = arrivalOrder;
            return true;
        }

        public bool TrySchedule(CombatMilestone milestone)
        {
            _ = new CombatMilestone(milestone.Id, milestone.TimeUs, milestone.Kind);
            if (IsSealed(milestone.TimeUs) || _pending.Count == _capacity ||
                _milestoneIds.Contains(milestone.Id)) return false;
            var arrivalOrder = checked(_lastArrivalOrder + 1);
            Insert(new CombatTimelineEvent(milestone, arrivalOrder));
            _milestoneIds.Add(milestone.Id);
            _lastArrivalOrder = arrivalOrder;
            return true;
        }

        public bool TryEnqueue(OrderedTouchRecord touch, long combatTimeUs)
        {
            if (combatTimeUs < 0) throw new ArgumentOutOfRangeException(nameof(combatTimeUs));
            _ = new OrderedTouchRecord(touch.Sample, touch.Owner, touch.Metrics);
            if (IsSealed(combatTimeUs) || _pending.Count == _capacity) return false;
            var arrival = checked(_lastArrivalOrder + 1);
            Insert(new CombatTimelineEvent(touch, combatTimeUs, arrival));
            _lastArrivalOrder = arrival;
            return true;
        }

        public long AllocateMilestoneId()
        {
            while (_nextGeneratedId > 0 && _milestoneIds.Contains(_nextGeneratedId)) _nextGeneratedId--;
            if (_nextGeneratedId == 0) throw new InvalidOperationException("Milestone identities exhausted.");
            return _nextGeneratedId--;
        }

        public bool TryGetMilestone(long id, out CombatMilestone milestone)
        {
            if (TryGetMilestoneRecord(id, out var record))
            {
                milestone = record.Milestone.Value;
                return true;
            }
            milestone = default;
            return false;
        }

        public bool TryGetMilestoneRecord(long id, out CombatTimelineEvent milestoneRecord)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            foreach (var record in _pending)
            {
                if (!record.Milestone.HasValue || record.Milestone.Value.Id != id) continue;
                milestoneRecord = record;
                return true;
            }
            milestoneRecord = default;
            return false;
        }

        public bool TryEnqueue(DefenseCommand command, long combatTimeUs)
        {
            if (combatTimeUs < 0) throw new ArgumentOutOfRangeException(nameof(combatTimeUs));
            _ = new DefenseCommand(command.Sequence, command.InputTimestampUs, command.Kind, command.GuardActionId);
            if (IsSealed(combatTimeUs) || _pending.Count == _capacity) return false;
            var arrival = checked(_lastArrivalOrder + 1);
            Insert(new CombatTimelineEvent(command, combatTimeUs, arrival));
            _lastArrivalOrder = arrival;
            return true;
        }

        public bool TryScheduleBatch(IReadOnlyList<CombatMilestone> milestones)
        {
            if (milestones == null) throw new ArgumentNullException(nameof(milestones));
            if (milestones.Count > _capacity - _pending.Count) return false;
            var ids = new HashSet<long>();
            foreach (var milestone in milestones)
            {
                _ = new CombatMilestone(milestone.Id, milestone.TimeUs, milestone.Kind);
                if (IsSealed(milestone.TimeUs) || _milestoneIds.Contains(milestone.Id) ||
                    !ids.Add(milestone.Id)) return false;
            }
            _ = checked(_lastArrivalOrder + milestones.Count);
            foreach (var milestone in milestones) TrySchedule(milestone);
            return true;
        }

        public bool TryGetNextImpact(out CombatMilestone impact)
        {
            var next = FindNextImpact();
            impact = next.GetValueOrDefault();
            return next.HasValue;
        }

        public bool TryCancelMilestone(long id)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            var index = _pending.FindIndex(value => value.Milestone.HasValue && value.Milestone.Value.Id == id);
            if (index < 0) return false;
            _pending.RemoveAt(index);
            _milestoneIds.Remove(id);
            return true;
        }

        public void AdvanceTo(long timeUs)
        {
            if (_advancing) throw new InvalidOperationException("Combat advancement is already active.");
            if (timeUs < TimeUs) throw new ArgumentOutOfRangeException(nameof(timeUs));
            _advancing = true;
            try
            {
                while (_pending.Count > 0 && _pending[0].TimeUs <= timeUs)
                {
                    var next = _pending[0];
                    _pending.RemoveAt(0);
                    SealAt(next.TimeUs);
                    // Phase owners still reference this record during pre-resolution handoff.
                    // Keep its identity reserved until all those callbacks have completed.
                    try { BeforeResolve?.Invoke(next); }
                    finally
                    {
                        if (next.Milestone.HasValue) _milestoneIds.Remove(next.Milestone.Value.Id);
                    }
                    Resolved?.Invoke(next);
                }
                SealAt(timeUs);
            }
            finally
            {
                _advancing = false;
            }
        }

        public long EnsureIncomingWarning(long minimumWarningUs = 650000)
        {
            if (minimumWarningUs < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumWarningUs));
            var nextImpact = FindNextImpact();
            if (!nextImpact.HasValue)
            {
                DiscardCommands();
                return -1;
            }
            var warningUs = nextImpact.Value.TimeUs - TimeUs;
            var shiftUs = warningUs < minimumWarningUs ? minimumWarningUs - warningUs : 0;
            ValidateShift(shiftUs);
            ShiftMilestonesAndDiscardCommands(shiftUs);
            return checked(nextImpact.Value.TimeUs + shiftUs);
        }

        public void DiscardCommands()
        {
            _pending.RemoveAll(value => value.Kind != CombatEventKind.Milestone);
        }

        private bool IsSealed(long timeUs)
        {
            return (_hasSealedTime && timeUs <= _sealedTimeUs) ||
                (_advancing && timeUs <= TimeUs);
        }

        private void SealAt(long timeUs)
        {
            TimeUs = timeUs;
            _sealedTimeUs = timeUs;
            _hasSealedTime = true;
        }

        private CombatMilestone? FindNextImpact()
        {
            foreach (var value in _pending)
            {
                if (value.Milestone.HasValue && value.Milestone.Value.Kind == CombatMilestoneKind.Impact)
                    return value.Milestone;
            }
            return null;
        }

        private void ValidateShift(long shiftUs)
        {
            foreach (var value in _pending)
                if (value.Milestone.HasValue) _ = checked(value.TimeUs + shiftUs);
        }

        private void ShiftMilestonesAndDiscardCommands(long shiftUs)
        {
            for (var index = _pending.Count - 1; index >= 0; index--)
            {
                var value = _pending[index];
                if (!value.Milestone.HasValue)
                {
                    _pending.RemoveAt(index);
                    continue;
                }
                if (shiftUs == 0) continue;
                var milestone = value.Milestone.Value;
                var shifted = new CombatMilestone(milestone.Id,
                    checked(milestone.TimeUs + shiftUs), milestone.Kind);
                _pending[index] = new CombatTimelineEvent(shifted, value.ArrivalOrder);
            }
        }

        private void Insert(CombatTimelineEvent value)
        {
            var lower = 0;
            var upper = _pending.Count;
            while (lower < upper)
            {
                var middle = lower + (upper - lower) / 2;
                if (Compare(_pending[middle], value) < 0) lower = middle + 1;
                else upper = middle;
            }
            _pending.Insert(lower, value);
        }

        private static int Compare(CombatTimelineEvent left, CombatTimelineEvent right)
        {
            var timeOrder = left.TimeUs.CompareTo(right.TimeUs);
            if (timeOrder != 0) return timeOrder;
            var priorityOrder = Priority(left).CompareTo(Priority(right));
            return priorityOrder != 0 ? priorityOrder : left.ArrivalOrder.CompareTo(right.ArrivalOrder);
        }

        private static int Priority(CombatTimelineEvent value)
        {
            if (value.Kind != CombatEventKind.Milestone) return 1;
            var kind = value.Milestone.Value.Kind;
            if (kind == CombatMilestoneKind.PlayerImpact) return 3;
            return kind == CombatMilestoneKind.Death ? 0 : (int)kind + 1;
        }

        private static void ValidateCommand(GestureCommand command)
        {
            _ = new GestureCommand(command.Sequence, command.InputTimestampUs, command.PointerId,
                command.Direction, command.Intent,
                new InteractionPhase(command.Phase.Id, command.Phase.Kind), command.Start, command.End);
        }
    }
}
