using System;

namespace CardCollection.Core
{
    public struct CardCollectionCompletedData
    {
        public string EventId;
    }
    
    public interface ICardCollectionCompletionNotifier
    {
        event Action<CardCollectionCompletedData> OnCollectionCompleted;
    }
}
