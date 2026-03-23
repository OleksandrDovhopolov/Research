using System;
using System.Collections.Generic;
using System.Linq;
using Resources.Core;

namespace CardCollectionImpl
{
    public static class ContentWidgetDataMapper
    {
        public static ContentWidgetData ToContentWidgetData(this CollectionRewardDefinition rewardDefinition)
        {
            if (rewardDefinition == null)
            {
                return ContentWidgetData.Empty;
            }

            IReadOnlyList<string> cardPackAddresses = new List<string>();

            if (rewardDefinition is DuplicatePointsChestOffer cardModel)
            {
                cardPackAddresses = cardModel.CardPack?
                    .Where(pack => pack != null && !string.IsNullOrWhiteSpace(pack.PackId))
                    .Select(pack => pack.PackId)
                    .ToArray() ?? Array.Empty<string>();
            }
            
            var resources = GetResources(rewardDefinition)?
                .Where(resource => resource != null && !string.IsNullOrWhiteSpace(resource.Type.ToString()))
                .Select(resource => new ContentWidgetResourceData(resource.Type.ToString(), Math.Max(1, resource.Amount)))
                .ToArray() ?? Array.Empty<ContentWidgetResourceData>();

            return new ContentWidgetData(cardPackAddresses, resources);
        }

        private static List<GameResource> GetResources(CollectionRewardDefinition rewardDefinition)
        {
            return rewardDefinition switch
            {
                DuplicatePointsChestOffer duplicatePointsChestOffer => duplicatePointsChestOffer.Resources,
                FullCollectionReward fullCollectionReward => fullCollectionReward.Resources,
                _ => null
            };
        }
    }
}
