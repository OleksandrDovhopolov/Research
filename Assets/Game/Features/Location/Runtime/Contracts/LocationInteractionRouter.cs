using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Locations
{
    //TODO does it should be in Assets/Game/Features/Location/Contracts/Location.Contracts.asmdef  / 
    // + Assets/Game/Features/Location/Runtime should have asmdef ? 
    public sealed class LocationInteractionRouter
    {
        private readonly Dictionary<string, ILocationInteractionHandler> _handlers = new(System.StringComparer.Ordinal);
        private ILocationInteractionHandler _fallbackHandler;

        public void RegisterHandler(string interactionKey, ILocationInteractionHandler handler)
        {
            if (string.IsNullOrWhiteSpace(interactionKey) || handler == null)
                return;

            _handlers[interactionKey] = handler;
        }

        public void RegisterFallbackHandler(ILocationInteractionHandler handler)
        {
            _fallbackHandler = handler;
        }

        public void UnregisterHandler(string interactionKey)
        {
            if (string.IsNullOrWhiteSpace(interactionKey))
                return;

            _handlers.Remove(interactionKey);
        }

        public bool Route(ILocationInteractable interactable, Vector3 worldPosition)
        {
            if (interactable == null || !interactable.IsInteractionEnabled)
                return false;

            var context = new LocationInteractionContext(interactable, worldPosition);
            if (string.IsNullOrWhiteSpace(context.InteractionKey))
            {
                Debug.LogWarning($"[LocationInteraction] Missing interaction key for '{context.InteractionId}'.");
                return TryHandleFallback(context);
            }

            if (_handlers.TryGetValue(context.InteractionKey, out var handler) && handler != null)
            {
                handler.Handle(context);
                return true;
            }

            Debug.LogWarning($"[LocationInteraction] No handler registered for key='{context.InteractionKey}', id='{context.InteractionId}'.");
            return TryHandleFallback(context);
        }

        private bool TryHandleFallback(LocationInteractionContext context)
        {
            if (_fallbackHandler == null)
                return false;

            _fallbackHandler.Handle(context);
            return true;
        }
    }
}
