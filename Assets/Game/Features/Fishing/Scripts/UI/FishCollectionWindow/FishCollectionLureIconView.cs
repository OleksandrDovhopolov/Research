using UIShared;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Fishing
{
    public sealed class FishCollectionLureIconView : MonoBehaviour, ICleanup
    {
        [SerializeField] private Image _icon;

        public string SpriteAddress { get; private set; }

        public void SetData(string spriteAddress)
        {
            SpriteAddress = spriteAddress ?? string.Empty;
        }

        public void SetSprite(Sprite sprite)
        {
            if (_icon != null)
                _icon.sprite = sprite;
        }

        public void Cleanup()
        {
            SpriteAddress = string.Empty;

            if (_icon != null)
                _icon.sprite = null;
        }
    }
}
