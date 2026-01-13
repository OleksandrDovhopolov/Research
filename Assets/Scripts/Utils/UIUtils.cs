using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public static  class UIUtils
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

        public static async UniTask LoadAndSetSpritesAsync<TConfig, TView>(
            List<TConfig> configs,
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
    }
}