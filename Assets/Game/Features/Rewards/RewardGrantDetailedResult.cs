namespace Rewards
{
    public enum RewardGrantFailureType
    {
        None = 0,
        Rejected = 1,
        Network = 2,
        Http = 3,
        InvalidResponse = 4,
        Unknown = 5
    }

    public sealed class RewardGrantDetailedResult
    {
        public bool Success { get; set; }
        public string RewardId { get; set; } = string.Empty;
        public int? NewCrystalsBalance { get; set; } // TODO what is it for ? reward cant be crystals all the time ? 
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public RewardGrantFailureType FailureType { get; set; } = RewardGrantFailureType.None;

        public static RewardGrantDetailedResult BuildSuccess(string rewardId, int? newCrystalsBalance)
        {
            return new RewardGrantDetailedResult
            {
                Success = true,
                RewardId = rewardId ?? string.Empty,
                NewCrystalsBalance = newCrystalsBalance,
                FailureType = RewardGrantFailureType.None
            };
        }

        public static RewardGrantDetailedResult BuildFailure(
            string rewardId,
            RewardGrantFailureType failureType,
            string errorCode,
            string errorMessage)
        {
            return new RewardGrantDetailedResult
            {
                Success = false,
                RewardId = rewardId ?? string.Empty,
                FailureType = failureType,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }
}
