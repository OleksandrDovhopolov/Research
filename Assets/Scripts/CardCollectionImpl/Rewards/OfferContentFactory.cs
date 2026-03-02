using System;
using System.Collections.Generic;
using CardCollection.Core;
using CardCollectionImpl;
using CollectionRewardDefinition = CardCollectionImpl.CollectionRewardDefinition;

namespace core
{
    public class OfferContentFactory : IOfferContentFactory
    {
        public CollectionRewardDefinition CreateFromGroupReward(GroupRewardDefinition groupRewardDefinition)
        {
            var content = new CardGroupCompletionReward
            {
                Source = RewardSource.GroupCompleted,
            };

            if (!TryCreateResource(groupRewardDefinition.RewardId, groupRewardDefinition.Amount, out var resource))
            {
                return content;
            }

            content.Resources.Add(resource);
            return content;
        }

        public CollectionRewardDefinition CreateFromCollectionReward(CardCollection.Core.CollectionRewardDefinition collectionRewardDefinition)
        {
            var content = new FullCollectionReward
            {
                Source = RewardSource.CollectionCompleted,
            };

            if (!TryCreateResource(collectionRewardDefinition.RewardId, collectionRewardDefinition.Amount, out var resource))
            {
                return content;
            }

            content.Resources.Add(resource);
            return content;
        }

        public CollectionRewardDefinition CreateFromExchangePack(ExchangePackEntry exchangePackEntry, IReadOnlyCollection<CardPack> cardPacks)
        {
            var content = new DuplicatePointsChestOffer
            {
                Source = RewardSource.ShopOffer,
            };

            if (cardPacks != null)
            {
                content.CardPack.AddRange(cardPacks);
            }

            if (exchangePackEntry?.RewardEntry?.ResourcesData == null)
            {
                return content;
            }

            foreach (var rewardData in exchangePackEntry.RewardEntry.ResourcesData)
            {
                if (rewardData == null)
                {
                    continue;
                }

                if (TryCreateResource(rewardData.ResourceId, rewardData.Amount, out var resource))
                {
                    content.Resources.Add(resource);
                }
            }

            return content;
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
