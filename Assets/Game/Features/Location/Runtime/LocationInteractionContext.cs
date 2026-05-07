using UnityEngine;

namespace Game.Features.Locations
{
    public readonly struct LocationInteractionContext
    {
        public LocationInteractionContext(ILocationInteractable interactable, Vector3 worldPosition)
        {
            Interactable = interactable;
            WorldPosition = worldPosition;
        }

        public ILocationInteractable Interactable { get; }
        public Vector3 WorldPosition { get; }
        public string InteractionId => Interactable?.InteractionId;
        public LocationInteractionType InteractionType => Interactable?.InteractionType ?? LocationInteractionType.Custom;
    }
}
