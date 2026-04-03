using System.Collections.Generic;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CollectionProgressSnapshotBuilder : ICollectionProgressSnapshotBuilder
    {
        private readonly ICardCollectionCacheService _cardCollectionCardCollectionCacheService;
        private readonly IReadOnlyList<CardCollectionGroupConfig> _cardCollectionGroupConfigs;
        
        public CollectionProgressSnapshotBuilder(ICardCollectionCacheService cardCollectionCacheService, IReadOnlyList<CardCollectionGroupConfig> groupsConfig)
        {
            _cardCollectionCardCollectionCacheService = cardCollectionCacheService;
            _cardCollectionGroupConfigs = groupsConfig;
        }
        
        public CollectionProgressSnapshot Build(EventCardsSaveData collectionData)
        {
            if (collectionData == null)
            {
                return default;
            }

            var groupProgress = BuildGroupProgress(collectionData);
            return new CollectionProgressSnapshot(
                collectionData.GetCollectedCardsAmount(),
                collectionData.Cards?.Count ?? 0,
                groupProgress);
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
