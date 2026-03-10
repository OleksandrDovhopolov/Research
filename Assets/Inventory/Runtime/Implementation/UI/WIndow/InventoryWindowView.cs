using System.Collections.Generic;
using Inventory.Implementation.UI;
using UIShared;
using UISystem;
using UnityEngine;

namespace Inventory.Implementation
{
    public class InventoryWindowView : WindowView
    {
        [SerializeField] private UIListPool<InventoryView> _cardGroupsPool;
        [SerializeField] private Sprite _sprite;

        public void CreateItems(IReadOnlyList<InventoryItemUiModel> data)
        {
            _cardGroupsPool.DisableNonActive();
            if (data == null)
            {
                return;
            }

            foreach (var item in data)
            {
                var inventoryView = _cardGroupsPool.GetNext();
                inventoryView.SetData(item, _sprite);
            }
        }
    }
}
