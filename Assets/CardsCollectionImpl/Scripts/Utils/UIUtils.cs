using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Rewards;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public static class UIUtils
    {
        public static RewardViewData CreateRewardViewData(CardCollectionRewardsConfigSO so, string groupType)
        {
            if (string.IsNullOrEmpty(groupType) || so.GroupRewards == null || so.GroupRewards.Length == 0)
            {
                Debug.LogWarning($"Failed to find groupType {groupType}. Empty returned");
                return RewardViewData.Empty;
            }

            foreach (var rewardDefinition in so.GroupRewards)
            {
                if (!string.Equals(rewardDefinition.GroupId, groupType, StringComparison.Ordinal))
                {
                    continue;
                }

                return new RewardViewData(rewardDefinition.RewardId, rewardDefinition.Icon, rewardDefinition.Amount);
            }

            return RewardViewData.Empty;
        }
        
        public static RewardViewData CreateRewardViewData(IRewardSpecProvider rewardSpecProvider, string groupType, CardCollectionRewardsConfigSO so)
        {
            if (string.IsNullOrEmpty(groupType) || so.GroupRewards == null)
                return RewardViewData.Empty;

            foreach (var groupReward in so.GroupRewards)
            {
                if (!string.Equals(groupReward.GroupId, groupType, StringComparison.Ordinal))
                    continue;

                if (!rewardSpecProvider.TryGet(groupReward.RewardId, out var spec))
                    return RewardViewData.Empty;

                return new RewardViewData(groupReward.RewardId, spec.Icon, spec.TotalAmountForUi);
            }

            return RewardViewData.Empty;
        }
        
        public static Vector2 ConvertWorldToLocalOfTargetParent(RectTransform source, RectTransform target)
        {
            var dstParent = (RectTransform)target.parent;

            var screenPos = RectTransformUtility.WorldToScreenPoint(null, source.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dstParent,
                screenPos,
                null,
                out var localPoint);

            return localPoint;
        }

        public static async UniTask BindAndSetSpritesAsync<TConfig>(
            IReadOnlyList<TConfig> configs,
            string eventId,
            IEventSpriteManager eventSpriteManager,
            Func<TConfig, string> getSpriteAddress,
            Func<TConfig, Image> getImage,
            CancellationToken cancellationToken = default)
        {
            var loadTasks = configs.Select(async config =>
            {
                try
                {
                    var spriteAddress = getSpriteAddress(config);
                    var image = getImage(config);
                    if (image == null)
                        return;

                    cancellationToken.ThrowIfCancellationRequested();
                    await eventSpriteManager.BindSpriteAsync(eventId, spriteAddress, image, cancellationToken);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed sprite : {e}");
                }
            });

            await UniTask.WhenAll(loadTasks);
            await UniTask.WaitForSeconds(0.5f);
        }
    }
}