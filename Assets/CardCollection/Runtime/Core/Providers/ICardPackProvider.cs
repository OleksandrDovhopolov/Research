using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface ICardPackProvider
    {
        UniTask<List<CardPackConfig>> GetCardPacksAsync(CancellationToken ct = default);

        UniTask<CardPackConfig> GetCardPackByIdAsync(string packId, CancellationToken ct = default);
    }
}
