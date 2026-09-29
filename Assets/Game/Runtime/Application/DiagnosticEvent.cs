using System;

namespace Praxen.Game.Application
{
    public readonly struct DiagnosticEvent
    {
        public DiagnosticCode Code { get; }
        public ApplicationState PreviousState { get; }
        public ApplicationState CurrentState { get; }

        public DiagnosticEvent(DiagnosticCode code, ApplicationState previousState,
            ApplicationState currentState)
        {
            if (!Enum.IsDefined(typeof(DiagnosticCode), code))
                throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown diagnostic code.");
            if (!Enum.IsDefined(typeof(ApplicationState), previousState))
                throw new ArgumentOutOfRangeException(nameof(previousState), previousState,
                    "Unknown previous application state.");
            if (!Enum.IsDefined(typeof(ApplicationState), currentState))
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState,
                    "Unknown current application state.");

            Code = code;
            PreviousState = previousState;
            CurrentState = currentState;
        }
    }
}
