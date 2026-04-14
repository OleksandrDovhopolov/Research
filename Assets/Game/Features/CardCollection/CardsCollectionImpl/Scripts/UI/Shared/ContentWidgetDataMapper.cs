using System;
using System.Linq;
using Rewards;

namespace CardCollectionImpl
{
    public static class ContentWidgetDataMapper
    {
        public static ContentWidgetData ToContentWidgetData(this RewardSpec rewardSpec)
        {
            if (rewardSpec == null)
            {
                return ContentWidgetData.Empty;
            }

            var resources = rewardSpec.Resources?
                .Where(resource => resource != null && !string.IsNullOrWhiteSpace(resource.ResourceId))
                .Select(resource => new ContentWidgetResourceData(resource.ResourceId, Math.Max(1, resource.Amount)))
                .ToArray() ?? Array.Empty<ContentWidgetResourceData>();

            return new ContentWidgetData(Array.Empty<string>(), resources);
        }
    }
}
