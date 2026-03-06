namespace CardCollection.Core
{
    public readonly struct CollectionProgressSnapshot
    {
        public readonly int CollectedAmount;
        public readonly int TotalAmount;

        public CollectionProgressSnapshot(int collectedAmount, int totalAmount)
        {
            CollectedAmount = collectedAmount;
            TotalAmount = totalAmount;
        }
    }
}
