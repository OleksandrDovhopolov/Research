using System;
using System.Collections.Generic;

namespace Rewards
{
    public sealed class RewardSpecInventoryItemCategoryResolver : IInventoryItemCategoryResolver
    {
        private const string FallbackCategoryId = "regular";
        private readonly Dictionary<string, string> _categoryByItemId;

        public RewardSpecInventoryItemCategoryResolver(RewardSpecsConfigSO rewardSpecsConfigSo)
        {
            _categoryByItemId = new Dictionary<string, string>(StringComparer.Ordinal);
            var rewardSpecs = rewardSpecsConfigSo?.RewardSpecs;
            if (rewardSpecs == null || rewardSpecs.Count == 0)
            {
                return;
            }

            for (var i = 0; i < rewardSpecs.Count; i++)
            {
                var reward = rewardSpecs[i];
                if (reward?.Resources == null || reward.Resources.Count == 0)
                {
                    continue;
                }

                for (var j = 0; j < reward.Resources.Count; j++)
                {
                    var resource = reward.Resources[j];
                    if (resource == null ||
                        resource.Kind != RewardKind.InventoryItem ||
                        string.IsNullOrWhiteSpace(resource.ResourceId))
                    {
                        continue;
                    }

                    var categoryId = string.IsNullOrWhiteSpace(resource.Category)
                        ? FallbackCategoryId
                        : resource.Category;
                    _categoryByItemId[resource.ResourceId] = categoryId;
                }
            }
        }

        public string ResolveCategoryId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return FallbackCategoryId;
            }

            return _categoryByItemId.TryGetValue(itemId, out var categoryId) && !string.IsNullOrWhiteSpace(categoryId)
                ? categoryId
                : FallbackCategoryId;
        }
    }
}
