using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class ContentItemView : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _amount;
        
        public void SetSprite(Sprite sprite)
        {
            _image.sprite = sprite;
        }
        
        public void SetText(string text)
        {
            _amount.text = text;
        }

        public float GetSpriteWidth()
        {
            return _image.sprite.rect.width;
        }
    }
}