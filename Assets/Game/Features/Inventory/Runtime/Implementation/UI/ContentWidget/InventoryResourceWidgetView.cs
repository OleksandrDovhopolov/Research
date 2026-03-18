using System.Collections;
using TMPro;
using UIShared;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory.Implementation
{
    public class InventoryResourceWidgetView : MonoBehaviour, IContentWidgetView
    {
        [SerializeField] private Button _inventoryButton;
        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _amountText;
        
        private InventoryResourceWidgetData _contentData;
        
        public bool Setup(ContentWidgetDataBase data)
        {
            _contentData = (InventoryResourceWidgetData)data;

            if (_contentData == null)
            {
                Debug.LogWarning($"Failed to create inventory widget data for {nameof(InventoryWidgetData)}");
                return false;
            }

            _image.sprite = _contentData.ItemSprite;
            _amountText.text = _contentData.ItemAmount.ToString();
            _inventoryButton.onClick.RemoveAllListeners();
            _inventoryButton.onClick.AddListener(OnInventoryButtonClickedHandler);

            return true;
        }

        public IEnumerator OnViewCreated()
        {
            yield return null;
        }

        private void OnInventoryButtonClickedHandler()
        {
            _contentData.ButtonPressed?.Invoke(_contentData.ItemId);
        }
    }
}
