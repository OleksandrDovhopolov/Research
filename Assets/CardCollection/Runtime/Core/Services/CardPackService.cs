using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core
{
    public class CardPackService
    {
        private readonly ICardPackProvider _cardPackProvider;
        private readonly Dictionary<string, CardPack> _packsByIdCache;
        private List<CardPack> _allPacks;
        private bool _isInitialized;

        public event Action<CardPack> OnPackPurchasedEvent;
        public event Action OnInitialized;

        public bool IsInitialized => _isInitialized;

        public CardPackService(ICardPackProvider cardPackProvider)
        {
            _cardPackProvider = cardPackProvider ?? throw new ArgumentNullException(nameof(cardPackProvider));
            _packsByIdCache = new Dictionary<string, CardPack>();
            _allPacks = new List<CardPack>();
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[CardCollectionService] Already initialized");
                return;
            }

            try
            {
                Debug.Log("[CardCollectionService] Initializing...");

                var packConfigs = await _cardPackProvider.GetCardConfigsAsync(ct);

                _allPacks = packConfigs.Select(config => new CardPack(config)).ToList();

                _packsByIdCache.Clear();
                foreach (var pack in _allPacks)
                {
                    _packsByIdCache[pack.PackId] = pack;
                }

                _isInitialized = true;
                Debug.Log($"[CardCollectionService] Initialized with {_allPacks.Count} packs");
                OnInitialized?.Invoke();
            }
            catch (OperationCanceledException)
            {
                throw;
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
            return new List<CardPack>(_allPacks);
        }
        
        public List<CardPack> GetAvailablePacks()
        {
            ValidateInitialized();
            return _allPacks.ToList();
        }
        
        public CardPack GetPackById(string packId)
        {
            ValidateInitialized();

            if (!_packsByIdCache.TryGetValue(packId, out var pack))
            {
                Debug.LogWarning($"[CardCollectionService] Pack not found: {packId}");
                return null;
            }

            return pack;
        }

        public List<CardPack> GetPacksByCardCount(int cardCount)
        {
            ValidateInitialized();
            return _allPacks.Where(p => p.CardCount == cardCount).ToList();
        }
        
        public void OnPackPurchased(string packId)
        {
            var pack = GetPackById(packId);
            if (pack != null)
            {
                pack.OnPurchased();
                OnPackPurchasedEvent?.Invoke(pack);
                Debug.Log($"[CardCollectionService] Pack purchased: {pack.PackName}");
            }
        }

        public (int totalPacks, int availablePacks, int totalPurchases) GetStatistics()
        {
            ValidateInitialized();
            var totalPurchases = _allPacks.Sum(p => p.PurchaseCount);
            var availablePacks = totalPurchases;
            return (_allPacks.Count, availablePacks, totalPurchases);
        }
        
        private void ValidateInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    "[CardCollectionService] Service not initialized. Call InitializeAsync() first.");
            }
        }

        public void Dispose()
        {
            OnPackPurchasedEvent = null;
            OnInitialized = null;
            _packsByIdCache?.Clear();
            _allPacks?.Clear();
            _isInitialized = false;
        }
    }
}
