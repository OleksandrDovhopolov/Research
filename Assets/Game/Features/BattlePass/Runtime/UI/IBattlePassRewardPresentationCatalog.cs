namespace BattlePass
{
    public interface IBattlePassRewardPresentationCatalog
    {
        bool TryGet(string rewardId, out BattlePassRewardPresentationDefinition rewardDefinition);
    }
}
