using Inventory.Implementation.UI;
using TMPro;
using UIShared;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory.Implementation
{
    public class InventoryView : MonoBehaviour, ICleanup
    {
        [SerializeField] private Image _image;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _subtitleText;
        [SerializeField] private Text _stackCountText;
        [SerializeField] private TextMeshProUGUI _stackCountTextPro;
        
        public void SetData(InventoryItemUiModel model)
        {
            if (_titleText != null)
            {
                _titleText.text = model.Title;
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = model.Subtitle;
            }

            if (_stackCountText != null)
            {
                _stackCountText.text = model.StackCount.ToString();
            }

            if (_stackCountTextPro != null)
            {
                _stackCountTextPro.gameObject.SetActive(model.StackCount > 0);
                _stackCountTextPro.text = model.StackCount.ToString();
            }
        }

        public void SetSprite(Sprite sprite)
        {
            _image.sprite = sprite;
        }

        public void Cleanup()
        {
            if (_image != null)
            {
                _image.sprite = null;
            }

            if (_titleText != null)
            {
                _titleText.text = string.Empty;
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = string.Empty;
            }

            if (_stackCountText != null)
            {
                _stackCountText.text = string.Empty;
            }

            if (_stackCountTextPro != null)
            {
                _stackCountTextPro.gameObject.SetActive(false);
                _stackCountTextPro.text = string.Empty;
            }
        }
    }
}
