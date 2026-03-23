using System.Collections.Generic;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CollectionProgressSnapshotService : ICollectionProgressSnapshotService
    {
        private bool _hasSnapshot;
        private CollectionProgressSnapshot _snapshot;

        private readonly IReadOnlyList<CardConfig> _cardCollectionConfigs;
        private readonly IReadOnlyList<CardCollectionGroupConfig> _cardCollectionGroupConfigs;
        
        public CollectionProgressSnapshotService(IReadOnlyList<CardConfig> cardConfigs, IReadOnlyList<CardCollectionGroupConfig> groupsConfig)
        {
            _cardCollectionConfigs = cardConfigs;
            _cardCollectionGroupConfigs = groupsConfig;
        }
        
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

        private List<CollectionProgressSnapshot.GroupProgressSnapshot> BuildGroupProgress(EventCardsSaveData collectionData)
        {
            var result = new List<CollectionProgressSnapshot.GroupProgressSnapshot>();
            var groups = _cardCollectionGroupConfigs;
            if (groups == null)
            {
                return result;
            }

            foreach (var groupsConfig in groups)
            {
                var groupType = groupsConfig.groupType;
                var groupName = groupsConfig.groupName;
                var totalGroupAmount = collectionData.GetGroupAmount(groupType, _cardCollectionConfigs);
                var collectedGroupAmount = collectionData.GetCollectedGroupAmount(groupType, _cardCollectionConfigs);

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
