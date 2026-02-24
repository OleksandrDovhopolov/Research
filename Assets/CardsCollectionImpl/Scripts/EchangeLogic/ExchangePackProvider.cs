using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace core
{
    public class ExchangePackProvider : IExchangePackProvider
    {
        private readonly Dictionary<string, ExchangePackEntry> _packById;
        
        public IReadOnlyCollection<ExchangePackEntry> GetAllPacks()
        {
            return _packById.Values.ToArray();
        }
        
        public ExchangePackProvider(ExchangePacksConfig packsConfig)
        {
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

        public Sprite GetPackSprite(string packId)
        {
            return TryGetPack(packId, out var pack) ? pack.Sprite : null;
        }

        public int GetPackPrice(string packId)
        {
            return TryGetPack(packId, out var pack) ? pack.PackPrice : 0;
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