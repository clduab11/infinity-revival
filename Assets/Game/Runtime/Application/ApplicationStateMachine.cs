using System;

namespace Praxen.Game.Application
{
    public sealed class ApplicationStateMachine
    {
        private readonly IDiagnosticsSink _diagnostics;
        private ApplicationState? _resumeTarget;

        public ApplicationState Current { get; private set; } = ApplicationState.Bootstrap;

        public ApplicationStateMachine(IDiagnosticsSink sink)
        {
            _diagnostics = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public bool TryTransition(ApplicationState next)
        {
            var previous = Current;
            if (!IsLegalTransition(next))
            {
                var rejected = new DiagnosticEvent(DiagnosticCode.TransitionRejected,
                    previous, previous);
                _diagnostics.Record(in rejected);
                return false;
            }

            _resumeTarget = next == ApplicationState.Suspended ? previous : (ApplicationState?)null;
            Current = next;
            var changed = new DiagnosticEvent(DiagnosticCode.StateChanged, previous, Current);
            _diagnostics.Record(in changed);
            return true;
        }

        private bool IsLegalTransition(ApplicationState next)
        {
            if (next == Current || !Enum.IsDefined(typeof(ApplicationState), next)
                || Current == ApplicationState.Shutdown)
                return false;
            if (next == ApplicationState.Shutdown || next == ApplicationState.Failed)
                return true;

            switch (Current)
            {
                case ApplicationState.Bootstrap:
                case ApplicationState.Failed:
                    return next == ApplicationState.Loading;
                case ApplicationState.Loading:
                    return next == ApplicationState.Refuge || next == ApplicationState.Suspended;
                case ApplicationState.Refuge:
                    return next == ApplicationState.Suspended;
                case ApplicationState.Suspended:
                    return next == _resumeTarget;
                default:
                    return false;
            }
        }
    }
}
