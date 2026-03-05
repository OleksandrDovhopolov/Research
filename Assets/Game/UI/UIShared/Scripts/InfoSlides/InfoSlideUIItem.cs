using System;
using UnityEngine;

namespace UIShared
{
    [Serializable]
    public class InfoSlideUIItem : SideUIItem
    {
        public float AppearDelay;
        public float Duration = 0.3f;
        public AnimationCurve AppearCurve;
    }
}