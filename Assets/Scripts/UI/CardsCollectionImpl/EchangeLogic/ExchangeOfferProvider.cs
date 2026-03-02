using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using CardCollectionImpl;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class ExchangeOfferProvider : IExchangeOfferProvider
    {
        private readonly UIManager _uiManager;
        private readonly Func<string, CancellationToken, UniTask<CardPackConfig>> _getCardConfigByIdAsync;
        private readonly ICardCollectionPointsAccount _cardCollectionPointsAccount;
        private readonly IOfferRewardsReceiver _offerRewardsReceiver;
        
        private readonly Dictionary<string, ExchangePackEntry> _packById;
        
        public ExchangeOfferProvider(
            ExchangePacksConfig packsConfig,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            Func<string, CancellationToken, UniTask<CardPackConfig>> getCardConfigByIdAsync,
            IOfferRewardsReceiver offerRewardsReceiver)
        {
            _cardCollectionPointsAccount = cardCollectionPointsAccount;
            _getCardConfigByIdAsync = getCardConfigByIdAsync;
            _offerRewardsReceiver = offerRewardsReceiver;
            _packById = new Dictionary<string, ExchangePackEntry>();

            if (packsConfig == null || packsConfig.Packs == null)
            {
                return;
            }

            foreach (var pack in packsConfig.Packs)
            {
                if (pack == null || string.IsNullOrWhiteSpace(pack.Id))
                {
                    continue;
                }

                _packById[pack.Id] = pack;
            }
        }
        
        public ExchangeOfferProvider(
            ExchangePacksConfig packsConfig,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            Func<string, CancellationToken, UniTask<CardPackConfig>> getCardConfigByIdAsync,
            IOfferRewardsReceiver offerRewardsReceiver,
            UIManager uiManager) : this(packsConfig, cardCollectionPointsAccount, getCardConfigByIdAsync, offerRewardsReceiver)
        {
            _uiManager = uiManager;
        }

        public IReadOnlyCollection<ExchangePackEntry> GetAllOffers()
        {
            return _packById.Values.ToArray();
        }

        public int GetOfferPrice(string offerPackId)
        {
            return TryGetPackEntry(offerPackId, out var pack) ? pack.PackPrice : 0;
        }

        public async UniTask<bool> ReceiveOfferContent(string offerPackId, CancellationToken ct = default)
        {
            const string infoText = "Pack received successfully";
            var infoArgs = new InfoWidgetArg(_uiManager, infoText);
            _uiManager.Show<InfoWidgetController>(infoArgs);
            
            var offerContent = await GetOfferContentAsync(offerPackId, ct);
            return await _offerRewardsReceiver.ReceiveRewardsAsync(offerContent, ct);
        }

        public async UniTask<bool> TrySpendCollectionPointsAsync(int pointsToSpend, CancellationToken ct = default)
        {
            return await _cardCollectionPointsAccount.TrySpendPointsAsync(pointsToSpend, ct);
        }

        private bool TryGetPackEntry(string packId, out ExchangePackEntry pack)
        {
            if (string.IsNullOrWhiteSpace(packId))
            {
                pack = null;
                return false;
            }

            return _packById.TryGetValue(packId, out pack);
        }
        
        public async UniTask<CardCollectionImpl.CollectionRewardDefinition> GetOfferContentAsync(string offerPackId, CancellationToken ct = default)
        {
            if (!TryGetPackEntry(offerPackId, out var exchangePackEntry))
            {
                return new DuplicatePointsChestOffer();
            }

            var cardPacks = await GetRewardCardPacksAsync(exchangePackEntry, ct);
            var resources = GetRewardResources(exchangePackEntry);

            return new DuplicatePointsChestOffer
            {
                Source = RewardSource.ShopOffer,
                Resources = resources,
                CardPack = cardPacks,
            };
        }

        public async UniTask<CardCollectionImpl.CollectionRewardDefinition> GetCollectionRewardData(CancellationToken ct = default)
        {
            var collectionRewardContent = new FullCollectionReward();
            collectionRewardContent.Source = RewardSource.CollectionCompleted;
            collectionRewardContent.Resources.Add(new GameResource(ResourceType.Gold, 1000));
            collectionRewardContent.Resources.Add(new GameResource(ResourceType.Gems, 50));
            collectionRewardContent.Resources.Add(new GameResource(ResourceType.Energy, 100));
            
            await UniTask.CompletedTask;
            return collectionRewardContent;
        }

        private async UniTask<List<CardPack>> GetRewardCardPacksAsync(ExchangePackEntry pack, CancellationToken ct)
        {
            if (pack?.RewardEntry?.CardPacks is not { Count: > 0 })
            {
                return new List<CardPack>();
            }

            var result = new List<CardPack>();
            foreach (var cardPackId in pack.RewardEntry.CardPacks)
            {
                var config = await _getCardConfigByIdAsync(cardPackId, ct);
                if (config == null)
                {
                    Debug.LogWarning($"Failed to load CardPackConfig with ID {cardPackId}");
                    continue;
                }

                result.Add(new CardPack(config));
            }

            return result;
        }

        private static List<GameResource> GetRewardResources(ExchangePackEntry pack)
        {
            if (pack?.RewardEntry is not ExchangePackCardsRewardEntrySO { ResourcesData: { Count: > 0 } } cardsReward)
            {
                return new List<GameResource>();
            }
            
            var mappedResources = cardsReward.ResourcesData
                .Where(resourceData => resourceData is { Amount: > 0 })
                .Select(TryCreateGameResource)
                .Where(resource => resource != null)
                .ToList();

            return mappedResources.Count > 0 ? mappedResources : new List<GameResource>();
        }

        private static GameResource TryCreateGameResource(ResourceRewardData data)
        {
            if (data is not { Amount: > 0 } || string.IsNullOrWhiteSpace(data.ResourceId))
            {
                return null;
            }

            if (!Enum.TryParse<ResourceType>(data.ResourceId, true, out var resourceType))
            {
                return null;
            }

            return new GameResource(resourceType, data.Amount);
        }
    }
}