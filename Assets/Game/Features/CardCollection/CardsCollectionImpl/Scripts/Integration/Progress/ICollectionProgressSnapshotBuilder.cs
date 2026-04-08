using CardCollection.Core;

namespace CardCollectionImpl
{
    public interface ICollectionProgressSnapshotBuilder
    {
        CollectionProgressSnapshot Build(EventCardsSaveData collectionData);
    }
}
