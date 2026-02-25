using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class ExchangePackProvider : IExchangePackProvider
    {
        private readonly Dictionary<string, ExchangePackEntry> _packById;
        private readonly ICardCollectionModule _cardCollectionModule;
        
        public ExchangePackProvider(ExchangePacksConfig packsConfig, ICardCollectionModule cardCollectionModule)
        {
            _cardCollectionModule = cardCollectionModule;
            _packById = new Dictionary<string, ExchangePackEntry>();

            if (packsConfig == null || packsConfig.Packs == null)
            {
                return;
            }

            foreach (var pack in packsConfig.Packs)
            {
                if (pack == null || string.IsNullOrWhiteSpace(pack.Id))
                {
                    continue;
                }

                _packById[pack.Id] = pack;
            }
        }

        public IReadOnlyCollection<ExchangePackEntry> GetAllPacks()
        {
            return _packById.Values.ToArray();
        }
        
        public Sprite GetPackSprite(string packId)
        {
            return TryGetPack(packId, out var pack) ? pack.Sprite : null;
        }

        public int GetPackPrice(string packId)
        {
            return TryGetPack(packId, out var pack) ? pack.PackPrice : 0;
        }

        public async UniTask<bool> TrySpendPointsAsync(int pointsToSpend, CancellationToken ct = default)
        {
            return await _cardCollectionModule.TrySpendPointsAsync(pointsToSpend, ct);
        }

        private bool TryGetPack(string packId, out ExchangePackEntry pack)
        {
            if (string.IsNullOrWhiteSpace(packId))
            {
                pack = null;
                return false;
            }

            return _packById.TryGetValue(packId, out pack);
        }
    }
}