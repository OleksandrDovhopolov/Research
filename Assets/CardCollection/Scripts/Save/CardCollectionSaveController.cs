using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public interface ICollectionUpdater
    {
        public UniTask UnlockCard(string cardId);
        public UniTask Save();
        public UniTask<EventCardsSaveData> Load();
        public UniTask Clear();
    }
    
    public interface ICardUpdater
    {
        public UniTask ResetNewFlagAsync(string cardId);
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

        public async UniTask Save()
        {
            if (string.IsNullOrEmpty(_defaultEventId))
                throw new InvalidOperationException("Event ID is not set. Set CurrentEventId before saving.");

            await EnsureInitializedAsync();

            try
            {
                var existingData = await _eventCardService.LoadAsync(_defaultEventId);
                
                if (existingData?.Cards == null || existingData.Cards.Count == 0)
                {
                    throw new InvalidOperationException($"No card collection data found for event {_defaultEventId}. Data must be initialized before saving.");
                }

                await _eventCardService.SaveAsync(existingData);
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

    }
}
