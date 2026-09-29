using System;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Application.Input
{
    public sealed class GesturePhaseContext : IInteractionPhaseSource
    {
        public InteractionPhase Current { get; private set; } = InteractionPhase.Inactive;

        public void SetPhase(InteractionPhase phase)
        {
            if (phase.Id <= Current.Id)
                throw new ArgumentOutOfRangeException(nameof(phase), phase,
                    "Phase identities must strictly increase.");
            Current = phase;
        }
    }
}
