using System.Collections.Generic;
using CardCollection.Core;
using Infrastructure;

namespace CardCollectionImpl
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

        public void SetSnapshot(EventCardsSaveData collectionData)
        {
            if (collectionData == null)
            {
                return;
            }

            var groupProgress = BuildGroupProgress(collectionData);
            _snapshot = new CollectionProgressSnapshot(
                collectionData.GetCollectedCardsAmount(),
                collectionData.Cards?.Count ?? 0,
                groupProgress);
            _hasSnapshot = true;
        }

        private static List<CollectionProgressSnapshot.GroupProgressSnapshot> BuildGroupProgress(EventCardsSaveData collectionData)
        {
            var result = new List<CollectionProgressSnapshot.GroupProgressSnapshot>();
            var groups = CardGroupsConfigStorage.Instance?.Data;
            if (groups == null)
            {
                return result;
            }

            foreach (var groupsConfig in groups)
            {
                var groupType = groupsConfig.GroupType;
                var groupName = groupsConfig.GroupName;
                var totalGroupAmount = collectionData.GetGroupAmount(groupType);
                var collectedGroupAmount = collectionData.GetCollectedGroupAmount(groupType);

                result.Add(new CollectionProgressSnapshot.GroupProgressSnapshot(
                    groupType,
                    groupName,
                    collectedGroupAmount,
                    totalGroupAmount));
            }

            return result;
        }
    }
}
