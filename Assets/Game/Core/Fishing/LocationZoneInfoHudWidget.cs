using UnityEngine;
using UIShared;

namespace Fishing
{
    public sealed class LocationZoneInfoHudWidget : MonoBehaviour, IHudWidget
    {
        [SerializeField] private Transform _itemsRoot;

        public Transform ItemsRoot => _itemsRoot != null ? _itemsRoot : transform;

        private void Reset()
        {
            _itemsRoot = transform;
        }
    }
}
