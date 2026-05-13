using UnityEngine;

namespace Fabros.TileEditor
{
    public class SetSpritePropertyHelper : EnumPropertyHelper
    {
        [SerializeField] private Sprite[] _sprites;
        [SerializeField] private SpriteRenderer _mainRenderer;

        protected override void OnValueChange(int arg0) => _mainRenderer.sprite = _sprites[arg0];
    }
}
