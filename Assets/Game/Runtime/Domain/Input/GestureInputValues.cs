using System;

namespace Praxen.Game.Domain.Input
{
    public enum SamplePhase { Began, Moved, Ended, Cancelled }
    public enum PointerOwnerKind { Gameplay, Ui, Excluded }
    public enum InteractionPhaseKind { Inactive, EnemySequence, PlayerOpening }
    public enum SwipeDirection { Left, Right, Up, Down }
    public enum GestureIntent { Parry, Attack }

    public readonly struct NormalizedPoint
    {
        public double X { get; }
        public double Y { get; }

        public NormalizedPoint(double x, double y)
        {
            InputValidation.Finite(x, nameof(x));
            InputValidation.Finite(y, nameof(y));
            X = x;
            Y = y;
        }
    }

    public readonly struct ScreenMetrics
    {
        public int Width { get; }
        public int Height { get; }

        public ScreenMetrics(int width, int height)
        {
            InputValidation.Positive(width, nameof(width));
            InputValidation.Positive(height, nameof(height));
            Width = width;
            Height = height;
        }
    }

    public readonly struct TouchSample
    {
        public long PointerId { get; }
        public SamplePhase Phase { get; }
        public NormalizedPoint Position { get; }
        public long TimestampUs { get; }

        public TouchSample(long pointerId, SamplePhase phase, NormalizedPoint position,
            long timestampUs)
        {
            InputValidation.Positive(pointerId, nameof(pointerId));
            InputValidation.Defined(phase, nameof(phase));
            InputValidation.Nonnegative(timestampUs, nameof(timestampUs));
            PointerId = pointerId;
            Phase = phase;
            Position = position;
            TimestampUs = timestampUs;
        }
    }

    public readonly struct PointerOwnership
    {
        public PointerOwnerKind Kind { get; }
        public ulong ControlId { get; }

        public static PointerOwnership Gameplay => new PointerOwnership(PointerOwnerKind.Gameplay);
        public static PointerOwnership Excluded => new PointerOwnership(PointerOwnerKind.Excluded);

        public PointerOwnership(PointerOwnerKind kind, ulong controlId = 0)
        {
            InputValidation.Defined(kind, nameof(kind));
            if ((kind == PointerOwnerKind.Ui) != (controlId != 0))
                throw new ArgumentOutOfRangeException(nameof(controlId), controlId,
                    "UI ownership requires a control identity; other ownership has none.");
            Kind = kind;
            ControlId = controlId;
        }
    }

    public readonly struct InteractionPhase
    {
        public long Id { get; }
        public InteractionPhaseKind Kind { get; }
        public static InteractionPhase Inactive => new InteractionPhase(0, InteractionPhaseKind.Inactive);

        public InteractionPhase(long id, InteractionPhaseKind kind)
        {
            InputValidation.Defined(kind, nameof(kind));
            InputValidation.Nonnegative(id, nameof(id));
            if (kind != InteractionPhaseKind.Inactive)
                InputValidation.Positive(id, nameof(id));
            Id = id;
            Kind = kind;
        }
    }

    public sealed class GestureTuning
    {
        public double TravelThreshold { get; }
        public long MaximumDurationUs { get; }
        public static GestureTuning Default { get; } = new GestureTuning();

        public GestureTuning(double travelThreshold = 0.06, long maximumDurationUs = 350000)
        {
            InputValidation.Finite(travelThreshold, nameof(travelThreshold));
            if (travelThreshold <= 0)
                throw new ArgumentOutOfRangeException(nameof(travelThreshold), travelThreshold,
                    "Travel threshold must be positive.");
            InputValidation.Positive(maximumDurationUs, nameof(maximumDurationUs));
            TravelThreshold = travelThreshold;
            MaximumDurationUs = maximumDurationUs;
        }
    }

    internal static class InputValidation
    {
        internal static void Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, value, "Value must be finite.");
        }

        internal static void Positive(long value, string name)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(name, value, "Value must be positive.");
        }

        internal static void Nonnegative(long value, string name)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(name, value, "Value must be nonnegative.");
        }

        internal static void Defined<T>(T value, string name) where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new ArgumentOutOfRangeException(name, value, "Unknown enum value.");
        }
    }
}
