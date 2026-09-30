using System;
using System.Collections.Generic;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Application.Combat
{
    public sealed class CombatSession
    {
        public CombatClock Clock { get; }
        public CombatTimeline Timeline { get; }
        public long RejectedCommandCount { get; private set; }
        private readonly List<DefenseCommand> rejectedReleases = new List<DefenseCommand>();
        private bool rejectedTouches;
        public event Action<long> IncomingWarningRearmed;
        public event Action Suspended;
        public event Action<long> FrameAdvanced;
        public event Action<DefenseCommand> GuardReleaseRejected;
        public event Action TouchInputRejected;

        public CombatSession(long deviceStartUs, int queueCapacity = 1024)
        {
            Clock = new CombatClock(deviceStartUs);
            Timeline = new CombatTimeline(queueCapacity);
        }

        public bool CheckForStall(long deviceNowUs)
        {
            if (!Clock.CheckForStall(deviceNowUs)) return false;
            Timeline.DiscardCommands();
            Suspended?.Invoke();
            return true;
        }

        public void Suspend(long deviceNowUs, CombatSuspensionReason reason)
        {
            var previous = Clock.State;
            Clock.Suspend(deviceNowUs, reason);
            Timeline.DiscardCommands();
            if (previous != CombatClockState.Suspended) Suspended?.Invoke();
        }

        public void BeginResume(long deviceNowUs, long countdownUs = CombatClock.DefaultCountdownUs) =>
            Clock.BeginResume(deviceNowUs, countdownUs);

        public void AdvanceFrame(long deviceNowUs, IReadOnlyList<GestureCommand> commands)
        {
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            var inputs = new List<CombatInput>(commands.Count);
            foreach (var command in commands)
            {
                inputs.Add(command.Sequence > 0 ? new CombatInput(command) : default);
            }
            AdvanceInputFrame(deviceNowUs, inputs);
        }

        public void AdvanceInputFrame(long deviceNowUs, IReadOnlyList<CombatInput> commands)
        {
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            var previous = Clock.State;
            Clock.Advance(deviceNowUs);
            if (Clock.State != CombatClockState.Running)
            {
                Timeline.DiscardCommands();
                RejectedCommandCount = checked(RejectedCommandCount + commands.Count);
                if (previous != CombatClockState.Suspended && Clock.State == CombatClockState.Suspended)
                    Suspended?.Invoke();
                return;
            }
            if (previous == CombatClockState.Countdown)
            {
                var incomingTime = Timeline.EnsureIncomingWarning();
                if (incomingTime >= 0) IncomingWarningRearmed?.Invoke(incomingTime);
            }
            QueueCommands(commands);
            Timeline.AdvanceTo(Clock.TimeUs);
            FrameAdvanced?.Invoke(Clock.TimeUs);
            foreach (var command in rejectedReleases) GuardReleaseRejected?.Invoke(command);
            rejectedReleases.Clear();
            // Reset after accepted samples resolve, otherwise an accepted Begin could recreate a rejected contact.
            bool resetContacts = rejectedTouches;
            rejectedTouches = false;
            if (resetContacts) TouchInputRejected?.Invoke();
        }

        private void QueueCommands(IReadOnlyList<CombatInput> commands)
        {
            foreach (var command in commands)
            {
                if (!command.IsValid ||
                    !Clock.TryMapTimestamp(command.SourceTimestampUs, out var combatTime) ||
                    !(command.Gesture.HasValue ? Timeline.TryEnqueue(command.Gesture.Value, combatTime) :
                        command.Defense.HasValue ? Timeline.TryEnqueue(command.Defense.Value, combatTime) :
                        Timeline.TryEnqueue(command.Touch.Value, combatTime)))
                {
                    RejectedCommandCount = checked(RejectedCommandCount + 1);
                    if (command.Touch.HasValue) rejectedTouches = true;
                    if (command.Defense.HasValue && command.Defense.Value.Kind == DefenseCommandKind.GuardRelease)
                        rejectedReleases.Add(command.Defense.Value);
                }
            }
        }
    }
}
