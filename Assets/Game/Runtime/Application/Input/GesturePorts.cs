using Praxen.Game.Domain.Input;

namespace Praxen.Game.Application.Input
{
    public interface IInteractionPhaseSource
    {
        InteractionPhase Current { get; }
    }

    public interface IPointerOwnershipResolver
    {
        PointerOwnership Resolve(in TouchSample begin, in ScreenMetrics metrics);
    }
}
