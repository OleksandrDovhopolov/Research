using Inventory.Implementation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory.Implementation
{
    public class InventoryView : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _subtitleText;
        [SerializeField] private Text _stackCountText;
        
        public void SetData(InventoryItemUiModel model, Sprite sprite)
        {
            _image.sprite = sprite;
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
        }
    }
}
