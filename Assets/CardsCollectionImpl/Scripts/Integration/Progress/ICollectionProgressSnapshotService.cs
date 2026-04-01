using CardCollection.Core;

namespace CardCollectionImpl
{
    public interface ICollectionProgressSnapshotService
    {
        bool TryGetSnapshot(out CollectionProgressSnapshot snapshot);
        void SetSnapshot(EventCardsSaveData collectionData);
    }
}
