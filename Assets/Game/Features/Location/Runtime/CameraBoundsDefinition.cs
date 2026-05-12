using UnityEngine;

namespace Game.Features.Locations
{
    [DisallowMultipleComponent]
    public sealed class CameraBoundsDefinition : MonoBehaviour
    {
        [SerializeField] private Transform _leftTop;
        [SerializeField] private Transform _rightTop;
        [SerializeField] private Transform _rightBottom;
        [SerializeField] private Transform _leftBottom;

        public bool TryGetBounds(out Rect bounds)
        {
            if (_leftTop == null || _rightTop == null || _rightBottom == null || _leftBottom == null)
            {
                bounds = default;
                return false;
            }

            var minX = Mathf.Min(_leftTop.position.x, _rightTop.position.x, _rightBottom.position.x, _leftBottom.position.x);
            var maxX = Mathf.Max(_leftTop.position.x, _rightTop.position.x, _rightBottom.position.x, _leftBottom.position.x);
            var minZ = Mathf.Min(_leftTop.position.z, _rightTop.position.z, _rightBottom.position.z, _leftBottom.position.z);
            var maxZ = Mathf.Max(_leftTop.position.z, _rightTop.position.z, _rightBottom.position.z, _leftBottom.position.z);

            bounds = Rect.MinMaxRect(minX, minZ, maxX, maxZ);
            return maxX > minX && maxZ > minZ;
        }
    }
}
