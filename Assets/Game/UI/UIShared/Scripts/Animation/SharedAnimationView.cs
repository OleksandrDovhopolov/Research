using UnityEngine;
using UnityEngine.UI;

namespace UIShared
{
    public class SharedAnimationView : MonoBehaviour
    {
        [SerializeField] private Image _image;

        public void SetSprite(Sprite sprite)
        {
            _image.sprite = sprite;
        }
    }
}