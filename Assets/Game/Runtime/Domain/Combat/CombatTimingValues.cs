using System;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Domain.Combat
{
    public enum CombatMilestoneKind
    {
        Death,
        Telegraph,
        Impact,
        PhaseTransition,
        RecoveryComplete,
        BufferedOffense,
        PlayerImpact
    }

    public enum CombatEventKind { Command, Milestone, DefenseCommand, Touch }

    public readonly struct CombatMilestone
    {
        public long Id { get; }
        public long TimeUs { get; }
        public CombatMilestoneKind Kind { get; }

        public CombatMilestone(long id, long timeUs, CombatMilestoneKind kind)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (timeUs < 0) throw new ArgumentOutOfRangeException(nameof(timeUs));
            if (!Enum.IsDefined(typeof(CombatMilestoneKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Id = id;
            TimeUs = timeUs;
            Kind = kind;
        }
    }

    public readonly struct CombatTimelineEvent
    {
        public CombatEventKind Kind { get; }
        public long TimeUs { get; }
        public long ArrivalOrder { get; }
        public GestureCommand? Command { get; }
        public DefenseCommand? DefenseCommand { get; }
        public CombatMilestone? Milestone { get; }
        public OrderedTouchRecord? Touch { get; }

        internal CombatTimelineEvent(GestureCommand command, long timeUs, long arrivalOrder)
        {
            Kind = CombatEventKind.Command;
            TimeUs = timeUs;
            ArrivalOrder = arrivalOrder;
            Command = command;
            DefenseCommand = null;
            Milestone = null;
            Touch = null;
        }

        internal CombatTimelineEvent(DefenseCommand command, long timeUs, long arrivalOrder)
        {
            Kind = CombatEventKind.DefenseCommand;
            TimeUs = timeUs;
            ArrivalOrder = arrivalOrder;
            Command = null;
            DefenseCommand = command;
            Milestone = null;
            Touch = null;
        }

        internal CombatTimelineEvent(CombatMilestone milestone, long arrivalOrder)
        {
            Kind = CombatEventKind.Milestone;
            TimeUs = milestone.TimeUs;
            ArrivalOrder = arrivalOrder;
            Command = null;
            DefenseCommand = null;
            Milestone = milestone;
            Touch = null;
        }

        internal CombatTimelineEvent(OrderedTouchRecord touch, long timeUs, long arrivalOrder)
        {
            Kind = CombatEventKind.Touch;
            TimeUs = timeUs;
            ArrivalOrder = arrivalOrder;
            Touch = touch;
            Command = null;
            DefenseCommand = null;
            Milestone = null;
        }
    }
}
