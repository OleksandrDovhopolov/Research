using System.Collections.Generic;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public interface IPendingGroupCompletionPresentationQueue
    {
        void Enqueue(IEnumerable<string> groupTypes);
        IReadOnlyList<CardCollectionGroupConfig> DequeueAll();
        void Clear();
    }
}