namespace BattlePass
{
    public interface IBattlePassRewardCatalog
    {
        bool TryGet(string rewardId, out BattlePassRewardDefinition rewardDefinition);
    }
}
