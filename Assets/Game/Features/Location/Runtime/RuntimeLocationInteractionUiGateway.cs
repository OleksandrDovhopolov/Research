using System;
using Cysharp.Threading.Tasks;
using Game.Fishing;
using UISystem;
using UnityEngine;

namespace Game.Features.Locations
{
    public sealed class RuntimeLocationInteractionUiGateway : ILocationInteractionUiGateway
    {
        private readonly UIManager _uiManager;
        private readonly IFishCollectionDataBuilder _fishCollectionDataBuilder;

        public RuntimeLocationInteractionUiGateway(UIManager uiManager, IFishCollectionDataBuilder fishCollectionDataBuilder)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _fishCollectionDataBuilder = fishCollectionDataBuilder ?? throw new ArgumentNullException(nameof(fishCollectionDataBuilder));
        }

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
            OpenFishCollectionAsync(context).Forget();
        }

        public void OpenCustom(LocationInteractionContext context)
        {
            Log(context, "Open custom interaction");
        }

        private async UniTaskVoid OpenFishCollectionAsync(LocationInteractionContext context)
        {
            try
            {
                var args = await _fishCollectionDataBuilder.BuildAsync();
                _uiManager.Show<FishCollectionController>(args);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[LocationInteraction] Failed to open fish collection: key='{GetInteractionKey(context)}', id='{GetInteractionId(context)}'. {exception}");
            }
        }

        private static void Log(LocationInteractionContext context, string action)
        {
            Debug.LogWarning($"[LocationInteraction] {action}: key='{GetInteractionKey(context)}', id='{GetInteractionId(context)}'. UI implementation is not connected yet.");
        }

        private static string GetInteractionKey(LocationInteractionContext context)
        {
            return context.InteractionKey ?? "Not found";
        }

        private static string GetInteractionId(LocationInteractionContext context)
        {
            return context.InteractionId ??  "Not found";
        }
    }
}
