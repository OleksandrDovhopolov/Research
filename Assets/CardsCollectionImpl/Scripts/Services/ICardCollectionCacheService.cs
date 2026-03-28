using System.Collections.Generic;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public interface ICardCollectionCacheService
    {
        void Initialize(IReadOnlyList<CardConfig> configs);
        IEnumerable<CardProgressData> GetCardsByGroupType(EventCardsSaveData saveData, string groupType);
        int GetGroupAmount(EventCardsSaveData saveData, string groupType);
        int GetCollectedGroupAmount(EventCardsSaveData saveData, string groupType);
        List<NewCardDisplayData> ToNewCardDisplayData(List<CardProgressData> cardsData);
    }
}