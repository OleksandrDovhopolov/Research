using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface IEventCardsStorage : IDisposable
    {
        UniTask InitializeAsync();
        UniTask<EventCardsSaveData> LoadAsync(string eventId);
        UniTask SaveAsync(EventCardsSaveData data);
        UniTask UnlockCardsAsync(string eventId, IReadOnlyCollection<string> cardIds);
        UniTask ClearCollectionAsync();
    }
}