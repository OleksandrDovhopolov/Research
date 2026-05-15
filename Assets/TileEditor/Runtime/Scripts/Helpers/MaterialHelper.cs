using UnityEngine;

namespace TileEditor
{
    [RequireComponent(typeof(MeshRenderer))]
    public class MaterialHelper : MonoBehaviour
    {
        private MeshRenderer _renderer;

        void Awake()
        {
            TryGetRenderer();
        }

        private void TryGetRenderer()
        {
            if (_renderer != null) return;

            _renderer = GetComponent<MeshRenderer>();
        }

        public void ChangeColor(Color color)
        {
            TryGetRenderer();
            _renderer.material.color = color;
        }
    }
}