namespace CardCollection.Core
{
    public interface ICollectionProgressSnapshotService
    {
        bool TryGetSnapshot(out CollectionProgressSnapshot snapshot);
        void SetSnapshot(int collectedAmount, int totalAmount);
    }
}
