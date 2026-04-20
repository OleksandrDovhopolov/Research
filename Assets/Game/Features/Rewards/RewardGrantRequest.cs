namespace Rewards
{
    public sealed class RewardGrantRequest
    {
        public RewardGrantRequest(string rewardId, RewardKind kind, int amount, string category = null)
        {
            RewardId = rewardId;
            Kind = kind;
            Amount = amount;
            Category = category;
        }

        public string RewardId { get; }
        public RewardKind Kind { get; }
        public int Amount { get; }
        public string Category { get; }
    }
}
