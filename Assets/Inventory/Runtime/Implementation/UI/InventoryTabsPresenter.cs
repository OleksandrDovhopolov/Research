using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Inventory.API;
using R3;

namespace Inventory.Implementation.UI
{
    public sealed class InventoryTabsPresenter : IDisposable
    {
        private readonly IInventoryService _inventoryService;
        private readonly string _ownerId;
        private IDisposable _inventoryChangedSubscription;

        public InventoryTabsPresenter(IInventoryService inventoryService, string ownerId)
        {
            _inventoryService = inventoryService;
            _ownerId = ownerId;
            RegularItems = new RegularItemsTabViewModel();
            CardPacks = new CardPacksTabViewModel();
        }

        public RegularItemsTabViewModel RegularItems { get; }
        public CardPacksTabViewModel CardPacks { get; }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var regularItems = await _inventoryService.GetItemsAsync(
                _ownerId,
                InventoryItemCategory.Regular,
                cancellationToken);

            var cardPacks = await _inventoryService.GetItemsAsync(
                _ownerId,
                InventoryItemCategory.CardPack,
                cancellationToken);

            RegularItems.Items.Value = MapRegularItems(regularItems);
            CardPacks.Items.Value = MapCardPacks(cardPacks);

            _inventoryChangedSubscription?.Dispose();
            _inventoryChangedSubscription = _inventoryService.OnInventoryChanged
                .Where(evt => evt.OwnerId == _ownerId)
                .Subscribe(evt =>
                {
                    RegularItems.Items.Value = MapRegularItems(evt.RegularItems);
                    CardPacks.Items.Value = MapCardPacks(evt.CardPacks);
                });
        }

        public void Dispose()
        {
            _inventoryChangedSubscription?.Dispose();
            RegularItems.Items?.Dispose();
            CardPacks.Items?.Dispose();
        }

        private static IReadOnlyList<InventoryItemUiModel> MapRegularItems(
            IReadOnlyList<InventoryItemView> source)
        {
            var mapped = new List<InventoryItemUiModel>(source.Count);
            foreach (var item in source)
            {
                mapped.Add(new InventoryItemUiModel(
                    item.ItemId,
                    item.ItemType,
                    item.StackCount));
            }

            return mapped;
        }

        private static IReadOnlyList<InventoryItemUiModel> MapCardPacks(
            IReadOnlyList<InventoryItemView> source)
        {
            var mapped = new List<InventoryItemUiModel>(source.Count);
            foreach (var item in source)
            {
                var subtitle = item.CardPackMetadata.HasValue
                    ? $"{item.CardPackMetadata.Value.CardsInside} cards"
                    : string.Empty;

                var title = item.CardPackMetadata.HasValue
                    ? item.CardPackMetadata.Value.PackName
                    : item.ItemType;

                mapped.Add(new InventoryItemUiModel(
                    item.ItemId,
                    title,
                    item.StackCount,
                    subtitle));
            }

            return mapped;
        }
    }
}
