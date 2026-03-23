using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface ICardsConfigProvider
    {
        UniTask<List<CardConfig>> GetCardGroupsConfigsAsync(string eventId, CancellationToken ct = default);
        void ClearCache();
    }
}