namespace Rewards
{
    public enum RewardAdFlowState
    {
        Idle = 0,
        InitializingAds = 1,
        LoadingAd = 2,
        Ready = 3,
        ShowingAd = 4,
        WaitingServerGrant = 5,
        Success = 6,
        Failed = 7
    }
}
