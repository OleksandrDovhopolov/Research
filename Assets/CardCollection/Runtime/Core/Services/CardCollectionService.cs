using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core.Services
{
    public class CardCollectionService
    {
        private readonly ICardPackProvider provider;
        private Dictionary<string, CardPack> packsByIdCache;
        private List<CardPack> allPacks;
        private bool isInitialized;

        public event Action<CardPack> OnPackPurchasedEvent;
        public event Action OnInitialized;

        public bool IsInitialized => isInitialized;

        public CardCollectionService(ICardPackProvider cardPackProvider)
        {
            provider = cardPackProvider ?? throw new ArgumentNullException(nameof(cardPackProvider));
            packsByIdCache = new Dictionary<string, CardPack>();
            allPacks = new List<CardPack>();
        }

        public async Task InitializeAsync()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[CardCollectionService] Already initialized");
                return;
            }

            try
            {
                Debug.Log("[CardCollectionService] Initializing...");

                var packConfigs = await provider.GetCardPacksAsync();

                allPacks = packConfigs.Select(config => new CardPack(config)).ToList();

                packsByIdCache.Clear();
                foreach (var pack in allPacks)
                {
                    packsByIdCache[pack.config.packId] = pack;
                }

                isInitialized = true;
                Debug.Log($"[CardCollectionService] Initialized with {allPacks.Count} packs");
                OnInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardCollectionService] Initialization failed: {ex.Message}");
                throw;
            }
        }

        public List<CardPack> GetAllPacks()
        {
            ValidateInitialized();
            return new List<CardPack>(allPacks);
        }
        
        public List<CardPack> GetAvailablePacks()
        {
            ValidateInitialized();
            return allPacks.ToList();
        }
        
        public CardPack GetPackById(string packId)
        {
            ValidateInitialized();

            if (!packsByIdCache.TryGetValue(packId, out var pack))
            {
                Debug.LogWarning($"[CardCollectionService] Pack not found: {packId}");
                return null;
            }

            return pack;
        }

        public List<CardPack> GetPacksByCardCount(int cardCount)
        {
            ValidateInitialized();
            return allPacks.Where(p => p.config.cardCount == cardCount).ToList();
        }
        
        public void OnPackPurchased(string packId)
        {
            var pack = GetPackById(packId);
            if (pack != null)
            {
                pack.OnPurchased();
                OnPackPurchasedEvent?.Invoke(pack);
                Debug.Log($"[CardCollectionService] Pack purchased: {pack.config.packName}");
            }
        }

        public (int totalPacks, int availablePacks, int totalPurchases) GetStatistics()
        {
            ValidateInitialized();
            var totalPurchases = allPacks.Sum(p => p.purchaseCount);
            var availablePacks = totalPurchases;
            return (allPacks.Count, availablePacks, totalPurchases);
        }
        
        private void ValidateInitialized()
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException(
                    "[CardCollectionService] Service not initialized. Call InitializeAsync() first.");
            }
        }

        public void Dispose()
        {
            OnPackPurchasedEvent = null;
            OnInitialized = null;
            packsByIdCache?.Clear();
            allPacks?.Clear();
            isInitialized = false;
        }
    }
}