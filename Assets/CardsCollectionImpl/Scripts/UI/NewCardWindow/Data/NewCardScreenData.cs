using System;
using System.Collections.Generic;

namespace CardCollectionImpl
{
    [Serializable]
    public sealed class NewCardScreenData
    {
        public string EventId { get; }
        public int Points { get; }
        public List<NewCardDisplayData> Cards { get; }

        public NewCardScreenData(string eventId, int points, List<NewCardDisplayData> cards)
        {
            EventId = eventId;
            Points = points;
            Cards = cards ?? new List<NewCardDisplayData>();
        }
    }
}
