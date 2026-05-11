using UnityEngine;

namespace Game.Features.Locations
{
    public sealed class UnknownLocationInteractionHandler : ILocationInteractionHandler
    {
        public void Handle(LocationInteractionContext context)
        {
            Debug.LogWarning($"[LocationInteraction] Unknown interaction key='{context.InteractionKey}', id='{context.InteractionId}'.");
        }
    }
}
