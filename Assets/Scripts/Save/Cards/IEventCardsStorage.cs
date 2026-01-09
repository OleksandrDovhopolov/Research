using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace core
{
    public interface IEventCardsStorage
    {
        UniTask InitializeAsync();
        UniTask<EventCardsSaveData> LoadAsync(string eventId);
        UniTask SaveAsync(EventCardsSaveData data);
        UniTask UnlockCardsAsync(string eventId, IReadOnlyCollection<string> cardIds);
        UniTask ClearCollectionAsync();
    }
}