using UnityEngine;

namespace Game.Features.Locations
{
    public interface ILocationInteractable
    {
        string InteractionId { get; }
        string InteractionKey { get; }
        Transform HudAnchor { get; }
        int Priority { get; }
        bool IsInteractionEnabled { get; }
    }
}
