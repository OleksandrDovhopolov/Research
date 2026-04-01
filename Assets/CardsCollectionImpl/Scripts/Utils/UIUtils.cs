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