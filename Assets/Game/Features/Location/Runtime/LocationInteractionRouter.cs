using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Locations
{
    public sealed class LocationInteractionRouter
    {
        private readonly List<ILocationInteractionHandler> _handlers = new();
        private readonly List<ILocationInteractionHandler> _fallbackHandlers = new();

        public void RegisterHandler(ILocationInteractionHandler handler, bool asFallback = false)
        {
            if (handler == null)
                return;

            var handlers = asFallback ? _fallbackHandlers : _handlers;
            if (!handlers.Contains(handler))
                handlers.Add(handler);
        }

        public void UnregisterHandler(ILocationInteractionHandler handler)
        {
            if (handler == null)
                return;

            _handlers.Remove(handler);
            _fallbackHandlers.Remove(handler);
        }

        public bool Route(ILocationInteractable interactable, Vector3 worldPosition)
        {
            if (interactable == null || !interactable.IsInteractionEnabled)
                return false;

            var context = new LocationInteractionContext(interactable, worldPosition);

            if (TryHandle(context, _handlers))
                return true;

            if (TryHandle(context, _fallbackHandlers))
                return true;

            Debug.LogWarning($"[LocationInteraction] No handler registered for {context.InteractionType} ({context.InteractionId}).");
            return false;
        }

        private static bool TryHandle(LocationInteractionContext context, IReadOnlyList<ILocationInteractionHandler> handlers)
        {
            for (var i = 0; i < handlers.Count; i++)
            {
                var handler = handlers[i];
                if (handler == null || !handler.CanHandle(context))
                    continue;

                handler.Handle(context);
                return true;
            }

            return false;
        }
    }
}
