using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIShared
{
    public class ContentItemView : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _amount;
        [SerializeField] private GameObject _loadingAnimationObject;
        
        public void SetSprite(Sprite sprite)
        {
            _image.sprite = sprite;
        }
        
        public void SetText(string text)
        {
            _amount.text = text;
        }

        public void SetLoadingActive(bool isActive)
        {
            if (_image != null)
            {
                _image.enabled = !isActive;
            }

            if (_loadingAnimationObject != null)
            {
                _loadingAnimationObject.SetActive(isActive);
            }
        }
    }
}