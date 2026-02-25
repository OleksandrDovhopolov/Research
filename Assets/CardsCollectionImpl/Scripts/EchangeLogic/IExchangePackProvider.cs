using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public interface IExchangePackProvider
    {
        IReadOnlyCollection<ExchangePackEntry> GetAllPacks();
        Sprite GetPackSprite(string packId);
        int GetPackPrice(string packId);
        UniTask<bool> TrySpendPointsAsync(int pointsToSpend, CancellationToken ct = default);
    }
}
