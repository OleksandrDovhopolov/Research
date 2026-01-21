using System;
using System.Collections.Generic;
using core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core
{
    /// <summary>
    /// High-level interface for updating the card collection for a single event.
    /// Unity projects can still use this MonoBehaviour adapter, but core logic lives in EventCardsService.
    /// </summary>
    public interface ICollectionUpdater
    {
        UniTask UnlockCard(string cardId);
        UniTask UnlockCard(List<string> cardId);
        UniTask Save();
        UniTask<EventCardsSaveData> Load();
        UniTask Clear();
        UniTask<List<CardProgressData>> GetCardsByIds(List<string> cardIds);
    }
    
    public interface ICardUpdater
    {
        UniTask ResetNewFlagAsync(string cardId);
    }
    
    public class CardCollectionSaveController : MonoBehaviour, ICollectionUpdater, ICardUpdater
    {
        [SerializeField] private string _defaultEventId = "test";
        
        private EventCardsService _eventCardService;
        private IEventCardsStorage _storage;
        
        private bool _isInitialized;

        private void Start()
        {
            _storage = new JsonEventCardsStorage();
            _eventCardService = new EventCardsService(_storage);
        }

        public void Initialize(IEventCardsStorage storage = null)
        {
            _storage = storage ?? new JsonEventCardsStorage();
            _eventCardService = new EventCardsService(_storage);
        }

        private async UniTask EnsureInitializedAsync()
        {
            if (_isInitialized && _eventCardService != null)
                return;

            if (_eventCardService == null)
            {
                _storage ??= new JsonEventCardsStorage();
                _eventCardService = new EventCardsService(_storage);
            }

            await _eventCardService.InitializeAsync();
            _isInitialized = true;
        }

        public async UniTask UnlockCard(List<string> cardId)
        {
            if (cardId == null || cardId.Count == 0)
                throw new ArgumentException("Card IDs collection cannot be null or empty", nameof(cardId));

            if (string.IsNullOrEmpty(_defaultEventId))
                throw new InvalidOperationException("Event ID is not set. Set CurrentEventId before unlocking cards.");

            await EnsureInitializedAsync();

            try
            {
                await _eventCardService.UnlockCardsAsync(_defaultEventId, cardId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardCollectionSaveController] Failed to unlock cards [{string.Join(", ", cardId)}] for event {_defaultEventId}: {ex}");
                throw;
            }
        }

        public async UniTask Save()
        {
            if (string.IsNullOrEmpty(_defaultEventId))
                throw new InvalidOperationException("Event ID is not set. Set CurrentEventId before saving.");

            await EnsureInitializedAsync();

            try
            {
                var cardCollectionData = new EventCardsSaveData { EventId = _defaultEventId };

                foreach (var cardCollectionConfig in CardCollectionConfigStorage.Instance.Data)
                {
                    var cardData = new CardProgressData { CardId = cardCollectionConfig.Id, IsUnlocked = false };
                    cardCollectionData.Cards.Add(cardData);
                }

                await _eventCardService.SaveAsync(cardCollectionData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardCollectionSaveController] Failed to save event {_defaultEventId}: {ex}");
                throw;
            }
        }

        public async UniTask<EventCardsSaveData> Load()
        {
            if (string.IsNullOrEmpty(_defaultEventId))
                throw new InvalidOperationException("Event ID is not set. Set CurrentEventId before loading.");

            await EnsureInitializedAsync();

            try
            {
                return await _eventCardService.LoadAsync(_defaultEventId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardCollectionSaveController] Failed to load event {_defaultEventId}: {ex}");
                throw;
            }
        }

        public async UniTask<EventCardsSaveData> GetCollectionData()
        {
            return await Load();
        }
        
        public async UniTask Clear()
        {
            await EnsureInitializedAsync();

            try
            {
                await _eventCardService.ClearCollectionAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardCollectionSaveController] Failed to clear collection: {ex}");
                throw;
            }
        }

        public async UniTask UnlockCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                throw new ArgumentException("Card ID cannot be null or empty", nameof(cardId));

            if (string.IsNullOrEmpty(_defaultEventId))
                throw new InvalidOperationException("Event ID is not set. Set CurrentEventId before unlocking cards.");

            await EnsureInitializedAsync();

            try
            {
                await _eventCardService.UnlockCardAsync(_defaultEventId, cardId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardCollectionSaveController] Failed to unlock card {cardId} for event {_defaultEventId}: {ex}");
                throw;
            }
        }
        
        public async UniTask ResetNewFlagAsync(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                throw new ArgumentException("Card ID cannot be null or empty", nameof(cardId));

            if (string.IsNullOrEmpty(_defaultEventId))
                throw new InvalidOperationException("Event ID is not set. Set CurrentEventId before resetting flags.");

            await EnsureInitializedAsync();

            try
            {
                await _eventCardService.ResetNewFlagAsync(_defaultEventId, cardId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardCollectionSaveController] Failed to reset new flag for card {cardId} in event {_defaultEventId}: {ex}");
                throw;
            }
        }

        public async UniTask<List<CardProgressData>> GetCardsByIds(List<string> cardIds)
        {
            if (cardIds == null || cardIds.Count == 0)
                throw new ArgumentException("Card IDs collection cannot be null or empty", nameof(cardIds));

            if (string.IsNullOrEmpty(_defaultEventId))
                throw new InvalidOperationException("Event ID is not set. Set CurrentEventId before getting cards.");

            await EnsureInitializedAsync();

            try
            {
                return await _eventCardService.GetCardsByIdsAsync(_defaultEventId, cardIds);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardCollectionSaveController] Failed to get cards by IDs [{string.Join(", ", cardIds)}] for event {_defaultEventId}: {ex}");
                throw;
            }
        }
    }
}
