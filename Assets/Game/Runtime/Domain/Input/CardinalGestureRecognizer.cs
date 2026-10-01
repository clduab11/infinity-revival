using System;
using System.Collections.Generic;

namespace Praxen.Game.Domain.Input
{
    public sealed class CardinalGestureRecognizer
    {
        private const double DirectionTieTolerance = 1e-12;
        private readonly GestureTuning _tuning;
        private readonly Dictionary<long, Contact> _contacts = new Dictionary<long, Contact>();
        private long _gameplayPointer;
        private long _sequence;
        private bool _hasScreenMetrics;
        private ScreenMetrics _screenMetrics;

        public CardinalGestureRecognizer(GestureTuning tuning = null)
        {
            _tuning = tuning ?? GestureTuning.Default;
        }

        public GestureCommand? Process(in TouchSample sample,
            in PointerOwnership ownershipAtBegin, in InteractionPhase currentPhase,
            in ScreenMetrics metrics)
        {
            Validate(sample, ownershipAtBegin, currentPhase, metrics);
            ObserveContext(currentPhase, metrics);
            if (!_contacts.TryGetValue(sample.PointerId, out var contact))
            {
                if (sample.Phase == SamplePhase.Began)
                    Begin(sample, ownershipAtBegin, currentPhase, metrics);
                return null;
            }

            if (sample.TimestampUs < contact.LastTimestampUs)
                contact.CanRecognize = false;
            else
                contact.LastTimestampUs = sample.TimestampUs;
            if (sample.Phase == SamplePhase.Began)
                return null;
            if (sample.Phase == SamplePhase.Cancelled)
            {
                Release(sample.PointerId);
                return null;
            }

            var command = Recognize(sample, contact);
            if (sample.Phase == SamplePhase.Ended)
                Release(sample.PointerId);
            return command;
        }

        public void CancelAll()
        {
            foreach (var contact in _contacts.Values)
                contact.CanRecognize = false;
        }

        public void ResetContacts()
        {
            _contacts.Clear();
            _gameplayPointer = 0;
            _hasScreenMetrics = false;
        }

        private static void Validate(in TouchSample sample, in PointerOwnership owner,
            in InteractionPhase phase, in ScreenMetrics metrics)
        {
            // Struct defaults bypass constructors, so validate the public input boundary too.
            _ = new TouchSample(sample.PointerId, sample.Phase, sample.Position, sample.TimestampUs);
            _ = new PointerOwnership(owner.Kind, owner.ControlId);
            _ = new InteractionPhase(phase.Id, phase.Kind);
            _ = new ScreenMetrics(metrics.Width, metrics.Height);
        }

        private void ObserveContext(in InteractionPhase phase, in ScreenMetrics metrics)
        {
            if (_hasScreenMetrics &&
                (_screenMetrics.Width != metrics.Width || _screenMetrics.Height != metrics.Height))
                CancelAll();
            _screenMetrics = metrics;
            _hasScreenMetrics = true;
            foreach (var contact in _contacts.Values)
            {
                if (contact.Phase.Id != phase.Id || contact.Phase.Kind != phase.Kind)
                    contact.CanRecognize = false;
            }
        }

        private void Begin(in TouchSample sample, in PointerOwnership owner,
            in InteractionPhase phase, in ScreenMetrics metrics)
        {
            var capturedOwner = owner;
            if (owner.Kind == PointerOwnerKind.Gameplay &&
                (phase.Kind == InteractionPhaseKind.EnemySequence ||
                    phase.Kind == InteractionPhaseKind.PlayerOpening))
            {
                if (_gameplayPointer != 0)
                    capturedOwner = PointerOwnership.Excluded;
                else
                    _gameplayPointer = sample.PointerId;
            }
            _contacts.Add(sample.PointerId, new Contact(sample, capturedOwner, phase, metrics));
        }

        private GestureCommand? Recognize(in TouchSample sample, Contact contact)
        {
            if (!contact.CanRecognize)
                return null;
            if (sample.TimestampUs - contact.BeginTimestampUs > _tuning.MaximumDurationUs)
            {
                contact.CanRecognize = false;
                return null;
            }

            var shorter = Math.Min(contact.Metrics.Width, contact.Metrics.Height);
            var dx = (sample.Position.X - contact.Start.X) * contact.Metrics.Width / shorter;
            var dy = (sample.Position.Y - contact.Start.Y) * contact.Metrics.Height / shorter;
            if (!CrossedThreshold(dx, dy))
                return null;
            contact.CanRecognize = false;
            var intent = contact.Phase.Kind == InteractionPhaseKind.EnemySequence
                ? GestureIntent.Parry : GestureIntent.Attack;
            return new GestureCommand(checked(++_sequence), sample.TimestampUs, sample.PointerId,
                Direction(dx, dy), intent, contact.Phase, contact.Start, sample.Position);
        }

        private bool CrossedThreshold(double dx, double dy)
        {
            var largest = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (largest > _tuning.TravelThreshold)
                return true;
            if (largest == 0)
                return false;
            var ratio = Math.Min(Math.Abs(dx), Math.Abs(dy)) / largest;
            return largest * Math.Sqrt(1 + ratio * ratio) > _tuning.TravelThreshold;
        }

        private static SwipeDirection Direction(double dx, double dy)
        {
            var ax = Math.Abs(dx);
            var ay = Math.Abs(dy);
            if (ax >= ay || ay - ax <= DirectionTieTolerance)
                return dx >= 0 ? SwipeDirection.Right : SwipeDirection.Left;
            return dy >= 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }

        private void Release(long pointerId)
        {
            _contacts.Remove(pointerId);
            if (_gameplayPointer == pointerId)
                _gameplayPointer = 0;
        }

        private sealed class Contact
        {
            internal readonly NormalizedPoint Start;
            internal readonly PointerOwnership Ownership;
            internal readonly InteractionPhase Phase;
            internal readonly ScreenMetrics Metrics;
            internal readonly long BeginTimestampUs;
            internal long LastTimestampUs;
            internal bool CanRecognize;

            internal Contact(in TouchSample begin, in PointerOwnership ownership,
                in InteractionPhase phase, in ScreenMetrics metrics)
            {
                Start = begin.Position;
                Ownership = ownership;
                Phase = phase;
                Metrics = metrics;
                BeginTimestampUs = begin.TimestampUs;
                LastTimestampUs = begin.TimestampUs;
                CanRecognize = ownership.Kind == PointerOwnerKind.Gameplay &&
                    (phase.Kind == InteractionPhaseKind.EnemySequence ||
                    phase.Kind == InteractionPhaseKind.PlayerOpening);
            }
        }
    }
}
