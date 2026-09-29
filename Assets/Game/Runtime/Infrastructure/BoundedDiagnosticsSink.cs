using System;
using Praxen.Game.Application;

namespace Praxen.Game.Infrastructure
{
    /// <summary>Stores a bounded chronological history for the application thread.</summary>
    public sealed class BoundedDiagnosticsSink : IDiagnosticsSink
    {
        private readonly DiagnosticEvent[] _events;
        private int _nextWriteIndex;

        public int Count { get; private set; }
        public long TotalRecorded { get; private set; }

        public BoundedDiagnosticsSink(int capacity = 128)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
                    "Diagnostics capacity must be positive.");
            _events = new DiagnosticEvent[capacity];
        }

        public void Record(in DiagnosticEvent diagnosticEvent)
        {
            _events[_nextWriteIndex] = diagnosticEvent;
            _nextWriteIndex = (_nextWriteIndex + 1) % _events.Length;
            if (Count < _events.Length)
                Count++;
            TotalRecorded++;
        }

        public DiagnosticEvent[] Snapshot()
        {
            var snapshot = new DiagnosticEvent[Count];
            var oldestIndex = Count == _events.Length ? _nextWriteIndex : 0;
            for (var i = 0; i < Count; i++)
                snapshot[i] = _events[(oldestIndex + i) % _events.Length];
            return snapshot;
        }
    }
}
