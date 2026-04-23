namespace Rewards
{
    public enum RewardGrantFlowResultType
    {
        Success = 0,
        AdNotReady = 1,
        AdCanceled = 2,
        AdFailed = 3,
        ServerFailed = 4,
        NetworkError = 5,
        UnknownError = 6
    }

    public sealed class RewardGrantFlowResult
    {
        public RewardGrantFlowResultType Type;
        public string ErrorCode;
        public string ErrorMessage;

        public static RewardGrantFlowResult Build(
            RewardGrantFlowResultType type,
            string errorCode = null,
            string errorMessage = null)
        {
            return new RewardGrantFlowResult
            {
                Type = type,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }
}
