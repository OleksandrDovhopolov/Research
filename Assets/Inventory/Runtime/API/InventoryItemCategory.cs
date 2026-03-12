namespace Inventory.API
{
    public abstract class ItemCategory
    {
        protected ItemCategory(string categoryId, string displayName = null)
        {
            CategoryId = string.IsNullOrWhiteSpace(categoryId)
                ? GetType().Name
                : categoryId;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? CategoryId
                : displayName;
        }

        public string CategoryId { get; }
        public string DisplayName { get; }
    }
}
