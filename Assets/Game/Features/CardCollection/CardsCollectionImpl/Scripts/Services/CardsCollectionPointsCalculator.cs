using CardCollection.Core;

namespace CardCollectionImpl
{
    public class CardsCollectionPointsCalculator : ICardPointsCalculator
    {
        public static readonly CardsCollectionPointsCalculator Instance = new();

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