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
        
        public ExchangePackProvider(ExchangePacksConfig packsConfig, ICardCollectionPointsAccount cardCollectionPointsAccount)
        {
            _cardCollectionPointsAccount = cardCollectionPointsAccount;
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
        
        public ExchangePackProvider(ExchangePacksConfig packsConfig, ICardCollectionPointsAccount cardCollectionPointsAccount, UIManager  uiManager) :  this(packsConfig, cardCollectionPointsAccount)
        {
            _uiManager = uiManager;
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

        public PackContent GetPackContent(string packId)
        {
            if (!TryGetPack(packId, out var pack))
            {
                return new BasePackContent();
            }

            var firstResourceType = GetResourceTypeByIndex(packId, 0);
            var secondResourceType = GetResourceTypeByIndex(packId, 1);

            return new BasePackContent
            {
                Resources = new List<GameResource>
                {
                    new(firstResourceType, GetResourceAmount(pack.PackPrice, 2)),
                    new(secondResourceType, GetResourceAmount(pack.PackPrice, 1)),
                },
                CardPack = new List<CardPack>
                {
                    CreateRewardCardPack(packId),
                },
            };
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

        private static ResourceType GetResourceTypeByIndex(string packId, int offset)
        {
            var allTypes = new[]
            {
                ResourceType.Gold,
                ResourceType.Energy,
                ResourceType.Gems,
            };

            var startIndex = Math.Abs(packId.GetHashCode()) % allTypes.Length;
            return allTypes[(startIndex + offset) % allTypes.Length];
        }

        private static int GetResourceAmount(int packPrice, int multiplier)
        {
            var safePrice = packPrice <= 0 ? 1 : packPrice;
            return safePrice * multiplier;
        }

        private static CardPack CreateRewardCardPack(string packId)
        {
            var config = new CardPackConfig
            {
                packId = $"{packId}_reward",
                packName = $"Reward {packId}",
                cardCount = 1,
                softCurrencyCost = 0,
                hardCurrencyCost = 0,
                availableCardRarities = new List<string>(),
            };

            return new CardPack(config);
        }
    }
}