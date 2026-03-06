using UnityEngine;

namespace CardCollection.Core
{
    public sealed class CollectionProgressSnapshotService : ICollectionProgressSnapshotService
    {
        private bool _hasSnapshot;
        private CollectionProgressSnapshot _snapshot;

        public bool TryGetSnapshot(out CollectionProgressSnapshot snapshot)
        {
            snapshot = _snapshot;
            return _hasSnapshot;
        }

        public void SetSnapshot(int collectedAmount, int totalAmount)
        {
            Debug.LogWarning($"Test SetSnapshot {collectedAmount} / ");
            _snapshot = new CollectionProgressSnapshot(collectedAmount, totalAmount);
            _hasSnapshot = true;
        }
    }
}
