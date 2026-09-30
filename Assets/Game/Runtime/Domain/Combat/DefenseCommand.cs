using System;

namespace Praxen.Game.Domain.Combat
{
    public enum DefenseCommandKind { GuardPress, GuardRelease, DodgeLeft, DodgeRight }

    public readonly struct DefenseCommand
    {
        public long Sequence { get; }
        public long InputTimestampUs { get; }
        public DefenseCommandKind Kind { get; }
        public long GuardActionId { get; }

        public DefenseCommand(long sequence, long inputTimestampUs, DefenseCommandKind kind, long guardActionId = 0)
        {
            if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            if (inputTimestampUs < 0) throw new ArgumentOutOfRangeException(nameof(inputTimestampUs));
            if (guardActionId < 0) throw new ArgumentOutOfRangeException(nameof(guardActionId));
            if (!Enum.IsDefined(typeof(DefenseCommandKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Sequence = sequence;
            InputTimestampUs = inputTimestampUs;
            Kind = kind;
            GuardActionId = guardActionId;
        }
    }
}
