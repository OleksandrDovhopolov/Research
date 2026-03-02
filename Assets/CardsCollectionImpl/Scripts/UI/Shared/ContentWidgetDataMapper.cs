using System;
using System.Linq;
using CardCollectionImpl;
using Resources.Core;
using UIShared;

namespace core
{
    public static class ContentWidgetDataMapper
    {
        public static ContentWidgetData ToContentWidgetData(this CollectionRewardDefinition rewardDefinition)
        {
            if (rewardDefinition == null)
            {
                return ContentWidgetData.Empty;
            }

            var cardPackAddresses = rewardDefinition.CardPack?
                .Where(pack => pack != null && !string.IsNullOrWhiteSpace(pack.PackId))
                .Select(pack => pack.PackId)
                .ToArray() ?? Array.Empty<string>();

            var resources = GetResources(rewardDefinition)?
                .Where(resource => resource != null && !string.IsNullOrWhiteSpace(resource.Type.ToString()))
                .Select(resource => new ContentWidgetResourceData(resource.Type.ToString(), Math.Max(1, resource.Amount)))
                .ToArray() ?? Array.Empty<ContentWidgetResourceData>();

            return new ContentWidgetData(cardPackAddresses, resources);
        }

        private static System.Collections.Generic.List<GameResource> GetResources(CollectionRewardDefinition rewardDefinition)
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
