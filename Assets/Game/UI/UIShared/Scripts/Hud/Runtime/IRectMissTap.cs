using System.Collections.Generic;
using UnityEngine;

namespace UIShared
{
    public interface IRectMissTap
    {
        bool OnMissTap();
        IEnumerable<RectTransform> GetRectTransform();
    }
}
