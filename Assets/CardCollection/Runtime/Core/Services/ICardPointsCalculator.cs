namespace CardCollection.Core
{
    public interface ICardPointsCalculator
    {
        int GetPoints(int stars, bool isPremium);
    }
}
