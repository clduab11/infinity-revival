using System;

namespace Praxen.Game.Domain.Input
{
    public readonly struct GestureCommand
    {
        public long Sequence { get; }
        public long InputTimestampUs { get; }
        public long PointerId { get; }
        public SwipeDirection Direction { get; }
        public GestureIntent Intent { get; }
        public InteractionPhase Phase { get; }
        public NormalizedPoint Start { get; }
        public NormalizedPoint End { get; }

        public GestureCommand(long sequence, long inputTimestampUs, long pointerId,
            SwipeDirection direction, GestureIntent intent, InteractionPhase phase,
            NormalizedPoint start, NormalizedPoint end)
        {
            InputValidation.Positive(sequence, nameof(sequence));
            InputValidation.Nonnegative(inputTimestampUs, nameof(inputTimestampUs));
            InputValidation.Positive(pointerId, nameof(pointerId));
            InputValidation.Defined(direction, nameof(direction));
            InputValidation.Defined(intent, nameof(intent));
            if (phase.Kind == InteractionPhaseKind.Inactive ||
                (phase.Kind == InteractionPhaseKind.EnemySequence && intent != GestureIntent.Parry) ||
                (phase.Kind == InteractionPhaseKind.PlayerOpening && intent != GestureIntent.Attack))
                throw new ArgumentOutOfRangeException(nameof(phase), phase,
                    "Command intent must match an active captured phase.");
            Sequence = sequence;
            InputTimestampUs = inputTimestampUs;
            PointerId = pointerId;
            Direction = direction;
            Intent = intent;
            Phase = phase;
            Start = start;
            End = end;
        }
    }
}
