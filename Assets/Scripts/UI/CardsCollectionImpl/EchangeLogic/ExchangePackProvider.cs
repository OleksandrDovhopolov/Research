using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class ExchangePackProvider : IExchangePackProvider
    {
        private readonly UIManager _uiManager;
        private readonly Dictionary<string, ExchangePackEntry> _packById;
        private readonly ICardCollectionPointsAccount _cardCollectionPointsAccount;
        private readonly ICardPackProvider _cardPackProvider;
        private readonly Dictionary<string, CardPackConfig> _cardPackConfigsById = new();
        private readonly SemaphoreSlim _cardPackConfigsSemaphore = new(1, 1);
        private bool _isCardPackConfigCacheLoaded;
        
        public ExchangePackProvider(ExchangePacksConfig packsConfig, ICardCollectionPointsAccount cardCollectionPointsAccount, ICardPackProvider cardPackProvider)
        {
            _cardCollectionPointsAccount = cardCollectionPointsAccount;
            _cardPackProvider = cardPackProvider;
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
        
        public ExchangePackProvider(ExchangePacksConfig packsConfig, ICardCollectionPointsAccount cardCollectionPointsAccount, ICardPackProvider cardPackProvider, UIManager uiManager) : this(packsConfig, cardCollectionPointsAccount, cardPackProvider)
        {
            _uiManager = uiManager;
        }

        public IReadOnlyCollection<ExchangePackEntry> GetAllPacks()
        {
            return _packById.Values.ToArray();
        }

        public int GetPackPrice(string packId)
        {
            return TryGetPack(packId, out var pack) ? pack.PackPrice : 0;
        }

        public bool ReceivePackContent(string packId)
        {
            const string infoText = "Pack received successfully";
            var infoArgs = new InfoWidgetArg(_uiManager, infoText);
            _uiManager.Show<InfoWidgetController>(infoArgs);
            //TODO task 
            // https://www.notion.so/Write-logic-for-pack-reward-collection-in-ExchangePackProvider-312511859db380278eeac6cd659ae47c?v=49ab588c8e164a33aa3b0ecd61d096d0&source=copy_link
            return true;
        }

        public async UniTask<bool> TrySpendPointsAsync(int pointsToSpend, CancellationToken ct = default)
        {
            return await _cardCollectionPointsAccount.TrySpendPointsAsync(pointsToSpend, ct);
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
        
        public async UniTask<PackContent> GetPackContentAsync(string packId, CancellationToken ct = default)
        {
            if (!TryGetPack(packId, out var pack))
            {
                return new BasePackContent();
            }

            var cardPacks = await GetRewardCardPacksAsync(pack, ct);
            var resources = GetRewardResources(pack);

            return new BasePackContent
            {
                Resources = resources,
                CardPack = cardPacks,
            };
        }
        
        private async UniTask<List<CardPack>> GetRewardCardPacksAsync(ExchangePackEntry pack, CancellationToken ct)
        {
            if (pack?.RewardEntry?.CardPacks is not { Count: > 0 })
            {
                return new List<CardPack>();
            }

            await EnsureCardPackConfigsLoadedAsync(ct);

            var result = new List<CardPack>();
            foreach (var cardPackId in pack.RewardEntry.CardPacks.Where(cardPackId => !string.IsNullOrWhiteSpace(cardPackId)))
            {
                if (_cardPackConfigsById.TryGetValue(cardPackId, out var config))
                {
                    result.Add(new CardPack(config));
                }
                else
                {
                    Debug.LogWarning($"[ExchangePackProvider] Reward card pack id '{cardPackId}' was not found in ICardPackProvider configs.");
                }
            }

            return result;
        }

        private static List<GameResource> GetRewardResources(ExchangePackEntry pack)
        {
            if (pack?.RewardEntry is not ExchangePackCardsRewardEntrySO { ResourcesData: { Count: > 0 } } cardsReward)
            {
                return new List<GameResource>();
            }
            
            var mappedResources = cardsReward.ResourcesData
                .Where(resourceData => resourceData is { Amount: > 0 })
                .Select(TryCreateGameResource)
                .Where(resource => resource != null)
                .ToList();

            return mappedResources.Count > 0 ? mappedResources : new List<GameResource>();
        }

        private static GameResource TryCreateGameResource(ResourceRewardData data)
        {
            if (data is not { Amount: > 0 } || string.IsNullOrWhiteSpace(data.ResourceId))
            {
                return null;
            }

            if (!Enum.TryParse<ResourceType>(data.ResourceId, true, out var resourceType))
            {
                return null;
            }

            return new GameResource(resourceType, data.Amount);
        }

        private async UniTask EnsureCardPackConfigsLoadedAsync(CancellationToken ct)
        {
            if (_isCardPackConfigCacheLoaded)
            {
                return;
            }

            await _cardPackConfigsSemaphore.WaitAsync(ct);
            try
            {
                if (_isCardPackConfigCacheLoaded)
                {
                    return;
                }

                ct.ThrowIfCancellationRequested();
                var configs = await _cardPackProvider.GetCardPacksAsync(ct);
                _cardPackConfigsById.Clear();

                foreach (var config in configs)
                {
                    if (config == null || string.IsNullOrWhiteSpace(config.packId))
                    {
                        continue;
                    }

                    _cardPackConfigsById[config.packId] = config;
                }

                _isCardPackConfigCacheLoaded = true;
            }
            finally
            {
                _cardPackConfigsSemaphore.Release();
            }
        }
    }
}