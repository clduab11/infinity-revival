namespace Praxen.Game.Domain.Input
{
    /// <summary>Source sample with Begin ownership and capture dimensions; phase is resolved in combat order.</summary>
    public readonly struct OrderedTouchRecord
    {
        public TouchSample Sample { get; }
        public PointerOwnership Owner { get; }
        public ScreenMetrics Metrics { get; }

        public OrderedTouchRecord(TouchSample sample, PointerOwnership owner, ScreenMetrics metrics)
        {
            _ = new TouchSample(sample.PointerId, sample.Phase, sample.Position, sample.TimestampUs);
            _ = new PointerOwnership(owner.Kind, owner.ControlId);
            _ = new ScreenMetrics(metrics.Width, metrics.Height);
            Sample = sample;
            Owner = owner;
            Metrics = metrics;
        }
    }
}
