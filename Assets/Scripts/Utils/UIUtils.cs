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
    }
}