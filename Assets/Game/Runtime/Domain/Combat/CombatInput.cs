using System;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Domain.Combat
{
    public readonly struct CombatInput
    {
        public GestureCommand? Gesture { get; }
        public DefenseCommand? Defense { get; }
        public OrderedTouchRecord? Touch { get; }
        public bool IsValid => Gesture.HasValue || Defense.HasValue || Touch.HasValue;
        public long SourceTimestampUs => Gesture?.InputTimestampUs ?? Defense?.InputTimestampUs ??
            Touch?.Sample.TimestampUs ?? 0;

        public CombatInput(GestureCommand gesture)
        {
            if (gesture.Sequence <= 0) throw new ArgumentOutOfRangeException(nameof(gesture));
            Gesture = gesture;
            Defense = null;
            Touch = null;
        }

        public CombatInput(DefenseCommand defense)
        {
            if (defense.Sequence <= 0) throw new ArgumentOutOfRangeException(nameof(defense));
            Defense = defense;
            Gesture = null;
            Touch = null;
        }

        public CombatInput(OrderedTouchRecord touch)
        {
            Touch = new OrderedTouchRecord(touch.Sample, touch.Owner, touch.Metrics);
            Gesture = null;
            Defense = null;
        }
    }
}
