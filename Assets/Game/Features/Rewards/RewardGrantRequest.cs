namespace Rewards
{
    public readonly struct RewardGrantRequest
    {
        public RewardGrantRequest(string rewardId, int amount, string category = "")
        {
            RewardId = rewardId;
            Amount = amount;
            Category = category;
        }

        public string RewardId { get; }
        public int Amount { get; }
        public string Category { get; }
    }
}
