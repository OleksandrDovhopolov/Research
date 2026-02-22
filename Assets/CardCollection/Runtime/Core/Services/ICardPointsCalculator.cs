namespace CardCollection.Core
{
    /// <summary>
    /// Strategy interface for calculating points awarded for a card based on its rarity.
    /// Clients can provide their own implementation via <see cref="CardCollectionModuleConfig"/>.
    /// </summary>
    public interface ICardPointsCalculator
    {
        int GetPoints(int stars, bool isPremium);
    }

    /// <summary>
    /// Default points calculation:
    /// Premium → 10, then 1★→1, 2★→2, 3★→3, 4★→5, 5★→10.
    /// </summary>
    public sealed class DefaultCardPointsCalculator : ICardPointsCalculator
    {
        public int GetPoints(int stars, bool isPremium)
        {
            if (isPremium)
                return 10;

            return stars switch
            {
                1 => 1,
                2 => 2,
                3 => 3,
                4 => 5,
                5 => 10,
                _ => 0
            };
        }
    }
}
