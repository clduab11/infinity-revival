using System;
using System.Collections.Generic;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Application.Input
{
    public sealed class ExplorationInputController
    {
        private const int MaximumContacts = 32;
        private const double TapThreshold = .025;
        private readonly Dictionary<long, Contact> _contacts = new Dictionary<long, Contact>();
        private long _phaseId;
        private long _gameplayPointer;
        private long _lastTimestamp;
        private bool _hasTimestamp;
        private bool _overflowQuarantined;

        public event Action<NormalizedPoint> DestinationTapped;
        public event Action<double, double> InspectionDragged;

        public ExplorationInputController(long phaseId) => Reset(phaseId);

        public void Reset(long phaseId)
        {
            if (phaseId <= 0) throw new ArgumentOutOfRangeException(nameof(phaseId));
            _phaseId = phaseId;
            _contacts.Clear();
            _gameplayPointer = 0;
            _hasTimestamp = false;
            _overflowQuarantined = false;
        }

        public void Process(OrderedTouchRecord record, InteractionPhase phase)
        {
            _ = new OrderedTouchRecord(record.Sample, record.Owner, record.Metrics);
            _ = new InteractionPhase(phase.Id, phase.Kind);
            if (_overflowQuarantined) return;
            var sample = record.Sample;
            var chronological = !_hasTimestamp || sample.TimestampUs >= _lastTimestamp;
            if (chronological) _lastTimestamp = sample.TimestampUs;
            _hasTimestamp = true;
            foreach (var existing in _contacts.Values)
                if (!chronological || phase.Id != _phaseId ||
                    phase.Kind != InteractionPhaseKind.Exploration ||
                    record.Metrics.Width != existing.Metrics.Width ||
                    record.Metrics.Height != existing.Metrics.Height)
                    existing.Valid = false;

            if (sample.Phase == SamplePhase.Began)
            {
                Begin(record, phase, chronological);
                return;
            }
            if (!_contacts.TryGetValue(sample.PointerId, out var contact)) return;
            if (sample.Phase == SamplePhase.Cancelled)
            {
                Release(sample.PointerId);
                return;
            }
            if (contact.Valid) Move(sample, contact);
            if (sample.Phase != SamplePhase.Ended) return;
            var tapped = contact.Valid && !contact.Dragging;
            Release(sample.PointerId);
            if (tapped) DestinationTapped?.Invoke(sample.Position);
        }

        private void Begin(OrderedTouchRecord record, InteractionPhase phase, bool chronological)
        {
            var sample = record.Sample;
            if (_contacts.TryGetValue(sample.PointerId, out var duplicate))
            {
                duplicate.Valid = false;
                return;
            }
            if (_contacts.Count >= MaximumContacts)
            {
                // Held identities cannot be safely forgotten or promoted after overflow.
                // Only an explicit lifecycle/mode Reset releases this fail-closed quarantine.
                foreach (var held in _contacts.Values) held.Valid = false;
                _overflowQuarantined = true;
                return;
            }
            var valid = chronological && phase.Id == _phaseId &&
                phase.Kind == InteractionPhaseKind.Exploration &&
                record.Owner.Kind == PointerOwnerKind.Gameplay && _gameplayPointer == 0;
            _contacts.Add(sample.PointerId, new Contact(sample.Position, record.Metrics, valid));
            if (valid) _gameplayPointer = sample.PointerId;
        }

        private void Move(TouchSample sample, Contact contact)
        {
            var shorter = Math.Min(contact.Metrics.Width, contact.Metrics.Height);
            var xScale = (double)contact.Metrics.Width / shorter;
            var yScale = (double)contact.Metrics.Height / shorter;
            var fromStartX = (sample.Position.X - contact.Start.X) * xScale;
            var fromStartY = (sample.Position.Y - contact.Start.Y) * yScale;
            var largest = Math.Max(Math.Abs(fromStartX), Math.Abs(fromStartY));
            if (largest > TapThreshold || largest > 0 && largest * Math.Sqrt(1 +
                Math.Pow(Math.Min(Math.Abs(fromStartX), Math.Abs(fromStartY)) / largest, 2)) > TapThreshold)
                contact.Dragging = true;
            var dx = (sample.Position.X - contact.Last.X) * xScale;
            var dy = (sample.Position.Y - contact.Last.Y) * yScale;
            contact.Last = sample.Position;
            if (contact.Dragging && (dx != 0 || dy != 0)) InspectionDragged?.Invoke(dx, dy);
        }

        private void Release(long pointerId)
        {
            _contacts.Remove(pointerId);
            if (_gameplayPointer == pointerId) _gameplayPointer = 0;
        }

        private sealed class Contact
        {
            internal readonly NormalizedPoint Start;
            internal readonly ScreenMetrics Metrics;
            internal NormalizedPoint Last;
            internal bool Valid;
            internal bool Dragging;

            internal Contact(NormalizedPoint start, ScreenMetrics metrics, bool valid)
            {
                Start = Last = start;
                Metrics = metrics;
                Valid = valid;
            }
        }
    }
}
