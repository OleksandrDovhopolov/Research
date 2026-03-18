namespace CardCollection.Core
{
    public readonly struct RewardGrantRequest
    {
        public RewardGrantRequest(string rewardId, int amount)
        {
            RewardId = rewardId;
            Amount = amount;
        }

        public string RewardId { get; }
        public int Amount { get; }
    }
}
