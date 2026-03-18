using System.Collections;
using UIShared;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory.Implementation
{
    public class InventoryWidgetView : MonoBehaviour, IContentWidgetView
    {
        [SerializeField] private Button _inventoryButton;
        
        private InventoryWidgetData _contentData;
        
        public bool Setup(ContentWidgetDataBase data)
        {
            _contentData = (InventoryWidgetData)data;

            if (_contentData == null)
            {
                Debug.LogWarning($"Failed to create inventory widget data for {nameof(InventoryWidgetData)}");
                return false;
            }
            
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
