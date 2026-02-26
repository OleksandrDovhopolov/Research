using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace core
{
    public interface IExchangePackProvider
    {
        IReadOnlyCollection<ExchangePackEntry> GetAllPacks();
        int GetPackPrice(string packId);
        UniTask<PackContent> GetPackContentAsync(string packId, CancellationToken ct = default);
        bool ReceivePackContent(string packId);
        UniTask<bool> TrySpendPointsAsync(int pointsToSpend, CancellationToken ct = default);
    }
}
