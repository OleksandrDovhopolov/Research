using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface ICardPackProvider
    {
        UniTask<List<CardPackConfig>> GetCardConfigsAsync(CancellationToken ct = default);
    }
}
