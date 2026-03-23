using System.Collections.Generic;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CollectionProgressSnapshotService : ICollectionProgressSnapshotService
    {
        private bool _hasSnapshot;
        private CollectionProgressSnapshot _snapshot;

        private readonly ICardCollectionCacheService _cardCollectionCardCollectionCacheService;
        private readonly IReadOnlyList<CardCollectionGroupConfig> _cardCollectionGroupConfigs;
        
        public CollectionProgressSnapshotService(ICardCollectionCacheService cardCollectionCacheService, IReadOnlyList<CardCollectionGroupConfig> groupsConfig)
        {
            _cardCollectionCardCollectionCacheService = cardCollectionCacheService;
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

            foreach (var groupsConfig in _cardCollectionGroupConfigs)
            {
                var groupType = groupsConfig.groupType;
                var groupName = groupsConfig.groupName;
                var totalGroupAmount = _cardCollectionCardCollectionCacheService.GetGroupAmount(collectionData, groupType);
                var collectedGroupAmount = _cardCollectionCardCollectionCacheService.GetCollectedGroupAmount(collectionData, groupType);

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
