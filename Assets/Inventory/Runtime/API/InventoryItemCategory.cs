using UnityEngine;

namespace Inventory.API
{
    public abstract class ItemCategory : ScriptableObject
    {
        [SerializeField] private string _categoryId;
        [SerializeField] private string _displayName;

        public string CategoryId => string.IsNullOrWhiteSpace(_categoryId) ? name : _categoryId;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? CategoryId : _displayName;

        protected void SetIdentity(string categoryId, string displayName)
        {
            _categoryId = categoryId;
            _displayName = displayName;
        }
    }

    public static class InventoryBuiltInCategoryIds
    {
        public const string Regular = "regular";
        public const string CardPack = "card_pack";
    }
}
