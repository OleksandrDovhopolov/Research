using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface ICardGroupsConfigProvider
    {
        UniTask<List<CardCollectionGroupConfig>> GetCardGroupsConfigsAsync(string eventId, CancellationToken ct = default);
        void ClearCache();
    }
}
