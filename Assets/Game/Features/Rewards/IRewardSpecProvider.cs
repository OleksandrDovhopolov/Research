namespace Rewards
{
    public interface IRewardSpecProvider
    {
        bool TryGet(string rewardId, out RewardSpec spec);
    }
}