namespace Game.Fishing
{
    public sealed class FishingHudLureViewData
    {
        public FishingHudLureViewData(
            string lureId,
            string displayName,
            string spriteAddress,
            string craftRecipeId,
            int count)
        {
            LureId = lureId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            SpriteAddress = spriteAddress ?? string.Empty;
            CraftRecipeId = craftRecipeId ?? string.Empty;
            Count = count;
        }

        public string LureId { get; }
        public string DisplayName { get; }
        public string SpriteAddress { get; }
        public string CraftRecipeId { get; }
        public int Count { get; }
    }
}
