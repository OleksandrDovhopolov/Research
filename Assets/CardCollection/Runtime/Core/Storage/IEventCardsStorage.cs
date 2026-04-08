using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface IEventCardsStorage : IDisposable
    {
        UniTask InitializeAsync(CancellationToken ct = default);
        UniTask<EventCardsSaveData> LoadAsync(string eventId, CancellationToken ct = default);
        UniTask SaveAsync(EventCardsSaveData data, CancellationToken ct = default);
        UniTask UnlockCardsAsync(EventCardsSaveData data, IReadOnlyCollection<string> cardIds, CancellationToken ct = default);
        UniTask DeleteAsync(string eventId, CancellationToken ct = default);
    }
}
