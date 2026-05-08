using UnityEngine;

namespace Game.Features.Locations
{
    public sealed class DefaultLocationInteractionUiGateway : ILocationInteractionUiGateway
    {
        public void OpenFishingTackleSelection(LocationInteractionContext context)
        {
            Log(context, "Open fishing tackle selection");
        }

        public void OpenFisherHouseProduction(LocationInteractionContext context)
        {
            Log(context, "Open fisher house production");
        }

        public void OpenChestItems(LocationInteractionContext context)
        {
            Log(context, "Open chest items");
        }

        public void OpenFishCollection(LocationInteractionContext context)
        {
            Log(context, "Open fish collection");
        }

        public void OpenCustom(LocationInteractionContext context)
        {
            Log(context, "Open custom interaction");
        }

        private static void Log(LocationInteractionContext context, string action)
        {
            Debug.LogWarning($"[LocationInteraction] {action}: key='{context.InteractionKey}', id='{context.InteractionId}'. UI implementation is not connected yet.");
        }
    }
}
