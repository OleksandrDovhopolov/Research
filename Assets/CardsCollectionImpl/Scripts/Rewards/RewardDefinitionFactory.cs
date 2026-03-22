using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Resources.Core;
using UnityEngine;

namespace CardCollectionImpl
{
    public class RewardDefinitionFactory : IRewardDefinitionFactory
    {
        //TODO change for Dictionary<string, CardPackConfig>
        private readonly List<CardPackConfig> _cardPackConfigs;
        private readonly Dictionary<string, ExchangePackEntry> _packById;

        public RewardDefinitionFactory(ExchangePacksConfig exchangePacksConfig, List<CardPackConfig> cardPackConfigs)
        {
            _cardPackConfigs = cardPackConfigs;
            _packById = new Dictionary<string, ExchangePackEntry>();
            
            if (exchangePacksConfig == null || exchangePacksConfig.Packs == null)
            {
                return;
            }

            foreach (var pack in exchangePacksConfig.Packs)
            {
                if (pack == null || string.IsNullOrWhiteSpace(pack.Id))
                {
                    continue;
                }

                _packById[pack.Id] = pack;
            }
        }
        
        public CollectionRewardDefinition CreateFromGroupReward(CollectionCompletionRewardConfig collectionCompletionRewardConfig)
        {
            var content = new CardGroupCompletionReward
            {
                Source = RewardSource.GroupCompleted,
            };

            if (!TryCreateResource(collectionCompletionRewardConfig.RewardId, collectionCompletionRewardConfig.Amount, out var resource))
            {
                return content;
            }

            content.Resources.Add(resource);
            return content;
        }

        public CollectionRewardDefinition CreateFromCollectionReward(FullCollectionRewardConfig fullCollectionRewardConfig)
        {
            var fullCollectionReward = new FullCollectionReward
            {
                Source = RewardSource.CollectionCompleted,
            };

            fullCollectionReward.Resources.Add(new GameResource(ResourceType.Gold, 1000));
            fullCollectionReward.Resources.Add(new GameResource(ResourceType.Gems, 50));
            fullCollectionReward.Resources.Add(new GameResource(ResourceType.Energy, 100));
            
            return fullCollectionReward;
        }
        
        public CollectionRewardDefinition CreateFromOfferReward(string offerPackId)
        {
            if (!TryGetPackEntry(offerPackId, out var exchangePackEntry))
            {
                return new DuplicatePointsChestOffer();
            }

            var cardPacks = GetRewardCardPacks(exchangePackEntry);
            var resources = GetRewardResources(exchangePackEntry);

            return new DuplicatePointsChestOffer
            {
                Source = RewardSource.CollectionPointsExchangeOffer,
                Resources = resources,
                CardPack = cardPacks,
            };
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
        
        private static List<GameResource> GetRewardResources(ExchangePackEntry pack)
        {
            if (pack?.RewardEntry is not ExchangePackCardsRewardEntrySO { ResourcesData: { Count: > 0 } } cardsReward)
            {
                //TODO create once EMPTY list and return it 
                return new List<GameResource>();
            }
            
            var mappedResources = cardsReward.ResourcesData
                .Where(resourceData => resourceData is { Amount: > 0 })
                .Select(resourceData =>
                    TryCreateResource(resourceData?.ResourceId, resourceData?.Amount ?? 0, out var resource)
                        ? resource
                        : null)
                .Where(resource => resource != null)
                .ToList();

            return mappedResources.Count > 0 ? mappedResources : new List<GameResource>();
        }
        
        private List<CardPack> GetRewardCardPacks(ExchangePackEntry pack)
        {
            if (pack?.RewardEntry?.CardPacks is not { Count: > 0 } || _cardPackConfigs == null)
            {
                return new List<CardPack>();
            }

            var result = new List<CardPack>();
            foreach (var cardPackId in pack.RewardEntry.CardPacks)
            {
                var config = _cardPackConfigs.FirstOrDefault(cfg =>
                    cfg != null && string.Equals(cfg.packId, cardPackId, StringComparison.Ordinal));

                if (config == null)
                {
                    Debug.LogWarning($"Failed to find CardPackConfig with ID {cardPackId}");
                    continue;
                }

                result.Add(new CardPack(config));
            }

            return result;
        }
        
        private static bool TryCreateResource(string resourceId, int amount, out GameResource resource)
        {
            resource = null;
            if (string.IsNullOrWhiteSpace(resourceId) || amount <= 0)
            {
                return false;
            }

            if (!Enum.TryParse<ResourceType>(resourceId, true, out var resourceType))
            {
                return false;
            }

            resource = new GameResource(resourceType, amount);
            return true;
        }
    }
}
