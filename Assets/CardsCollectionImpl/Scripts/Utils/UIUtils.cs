using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;

namespace CardCollectionImpl
{
    public static class UIUtils
    {
        public static RewardViewData CreateRewardViewData(CardCollectionRewardsConfigSO so, string groupType)
        {
            if (string.IsNullOrEmpty(groupType) || so.GroupRewards == null || so.GroupRewards.Length == 0)
            {
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

        public static async UniTask LoadAndSetSpritesAsync<TConfig, TView>(
            IReadOnlyList<TConfig> configs,
            Func<TConfig, string> getSpriteAddress,
            Func<TConfig, TView> getView,
            Action<TView, Sprite> setSprite,
            Func<TConfig, string> getErrorIdentifier)
        {
            var loadTasks = configs.Select(async config =>
            {
                try
                {
                    var spriteAddress = getSpriteAddress(config);
                    var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(spriteAddress);
                    var view = getView(config);
                    if (view != null)
                        setSprite(view, sprite);
                }
                catch (Exception e)
                {
                    var identifier = getErrorIdentifier(config);
                    Debug.LogError($"Failed sprite {identifier}: {e}");
                }
            });

            await UniTask.WhenAll(loadTasks);
            await UniTask.WaitForSeconds(0.5f);
        }
        
        public static async UniTask SetSprite(CardConfig config, CollectionCardView view, CancellationToken cancellationToken = default)
        {
            var task = ProdAddressablesWrapper.LoadAsync<Sprite>(config.icon);
            var sprite = await task.AsUniTask().AttachExternalCancellation(cancellationToken);
            view.SetCardImage(sprite);
        }
    }
}