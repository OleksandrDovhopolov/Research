using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public interface ICollectionUpdater
    {
        public UniTask OpenCard(string cardId);
        public UniTask Save();
        public UniTask Load();
        public UniTask Clear();
    }
    
    public class CardCollectionSaveController : MonoBehaviour, ICollectionUpdater
    {
        private const string TestEventId = "test";
        
        private EventCardsService _eventCardService;
        
        public async UniTask Save()
        {
            var cardCollectionData = new EventCardsSaveData{ EventId = TestEventId };
            
            foreach (var cardCollectionConfig in CardCollectionConfigStorage.Instance.Data)
            {
                var cardData = new CardProgressData { CardId = cardCollectionConfig.Id, IsUnlocked = false };
                cardCollectionData.Cards.Add(cardData);
            }

            var storage = await GetCardService();
            await storage.SaveAsync(cardCollectionData);
        }
        
        public async UniTask Load()
        {
            var storage = await GetCardService();
            await storage.LoadAsync(TestEventId);
        }

        public async UniTask<EventCardsSaveData> GetCollectionData(string eventId)
        {
            var storage = await GetCardService();
           return await storage.LoadAsync(eventId);
        }
        
        public async UniTask Clear()
        {
            var storage = await GetCardService();
            await storage.ClearCollectionAsync();
        }

        private async UniTask<EventCardsService> GetCardService()
        {
            if (_eventCardService != null) return _eventCardService;
            
            _eventCardService = new EventCardsService(new JsonEventCardsStorage());
            
            await _eventCardService.InitializeAsync();

            return _eventCardService;
        }

        public async UniTask OpenCard(string cardId)
        {
            Debug.LogWarning($"Debug OpenCard {cardId}");
            var storage = await GetCardService();
            await storage.UnlockCardAsync(TestEventId, cardId);
        }
    }
}