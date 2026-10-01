using System;

namespace Praxen.Game.Domain.Combat
{
    public enum CombatClockState
    {
        Running,
        Suspended,
        Countdown
    }

    public enum CombatSuspensionReason
    {
        None,
        FocusLost,
        ApplicationPaused,
        FrameStall,
        UserPaused
    }

    public sealed class CombatClock
    {
        public const long StallThresholdUs = 150000;
        public const long DefaultCountdownUs = 3000000;

        private long _segmentDeviceOriginUs;
        private long _segmentCombatOriginUs;
        private long _countdownDeadlineUs;

        public long TimeUs { get; private set; }
        public long LastDeviceTimeUs { get; private set; }
        public CombatClockState State { get; private set; }
        public CombatSuspensionReason SuspensionReason { get; private set; }
        public long CountdownRemainingUs { get; private set; }

        public CombatClock(long deviceStartUs)
        {
            if (deviceStartUs < 0)
                throw new ArgumentOutOfRangeException(nameof(deviceStartUs));

            LastDeviceTimeUs = deviceStartUs;
            _segmentDeviceOriginUs = deviceStartUs;
            State = CombatClockState.Running;
        }

        public bool CheckForStall(long deviceNowUs)
        {
            ValidateDeviceTime(deviceNowUs);
            if (State == CombatClockState.Suspended ||
                deviceNowUs - LastDeviceTimeUs <= StallThresholdUs)
                return false;

            Suspend(deviceNowUs, CombatSuspensionReason.FrameStall);
            return true;
        }

        public void Advance(long deviceNowUs)
        {
            if (CheckForStall(deviceNowUs))
                return;

            if (State == CombatClockState.Suspended)
            {
                LastDeviceTimeUs = deviceNowUs;
                return;
            }

            if (State == CombatClockState.Countdown)
            {
                AdvanceCountdown(deviceNowUs);
                return;
            }

            long nextTimeUs = checked(TimeUs + (deviceNowUs - LastDeviceTimeUs));
            TimeUs = nextTimeUs;
            LastDeviceTimeUs = deviceNowUs;
        }

        public void Suspend(long deviceNowUs, CombatSuspensionReason reason)
        {
            ValidateDeviceTime(deviceNowUs);
            if (reason != CombatSuspensionReason.FocusLost &&
                reason != CombatSuspensionReason.ApplicationPaused &&
                reason != CombatSuspensionReason.FrameStall &&
                reason != CombatSuspensionReason.UserPaused)
                throw new ArgumentOutOfRangeException(nameof(reason));

            LastDeviceTimeUs = deviceNowUs;
            State = CombatClockState.Suspended;
            SuspensionReason = reason;
            CountdownRemainingUs = 0;
            _countdownDeadlineUs = 0;
        }

        public void BeginResume(long deviceNowUs, long countdownUs = DefaultCountdownUs)
        {
            ValidateDeviceTime(deviceNowUs);
            if (State != CombatClockState.Suspended)
                throw new InvalidOperationException("The combat clock must be suspended before resuming.");
            if (countdownUs <= 0)
                throw new ArgumentOutOfRangeException(nameof(countdownUs));

            long deadlineUs = checked(deviceNowUs + countdownUs);
            LastDeviceTimeUs = deviceNowUs;
            _countdownDeadlineUs = deadlineUs;
            CountdownRemainingUs = countdownUs;
            State = CombatClockState.Countdown;
        }

        public bool TryMapTimestamp(long sourceTimestampUs, out long combatTimestampUs)
        {
            combatTimestampUs = 0;
            if (State != CombatClockState.Running ||
                sourceTimestampUs < _segmentDeviceOriginUs ||
                sourceTimestampUs > LastDeviceTimeUs)
                return false;

            try
            {
                combatTimestampUs = checked(_segmentCombatOriginUs +
                    (sourceTimestampUs - _segmentDeviceOriginUs));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        private void AdvanceCountdown(long deviceNowUs)
        {
            if (deviceNowUs < _countdownDeadlineUs)
            {
                CountdownRemainingUs = _countdownDeadlineUs - deviceNowUs;
                LastDeviceTimeUs = deviceNowUs;
                return;
            }

            LastDeviceTimeUs = deviceNowUs;
            _segmentDeviceOriginUs = deviceNowUs;
            _segmentCombatOriginUs = TimeUs;
            _countdownDeadlineUs = 0;
            CountdownRemainingUs = 0;
            SuspensionReason = CombatSuspensionReason.None;
            State = CombatClockState.Running;
        }

        private void ValidateDeviceTime(long deviceNowUs)
        {
            if (deviceNowUs < 0 || deviceNowUs < LastDeviceTimeUs)
                throw new ArgumentOutOfRangeException(nameof(deviceNowUs));
        }
    }
}
