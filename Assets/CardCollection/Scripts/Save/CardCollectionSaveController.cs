using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public interface ICollectionUpdater
    {
        public UniTask OpenCard(string cardId);
    }
    
    public class CardCollectionSaveController : MonoBehaviour, ICollectionUpdater
    {
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _clearButton;
        
        private const string TestEventId = "test";
        
        private EventCardsService _eventCardService;

        private void Start()
        {
            _saveButton.onClick.AddListener(() => Save().Forget());
            _loadButton.onClick.AddListener(() => Load().Forget());
            _clearButton.onClick.AddListener(() => Clear().Forget());
        }
        
        private async UniTask Save()
        {
            var cardCollectionData = new EventCardsSaveData{ EventId = TestEventId };
            
            foreach (var cardCollectionConfig in CardCollectionConfigStorage.Instance.Data)
            {
                var cardData = new CardProgressData { CardId = cardCollectionConfig.Id, IsUnlocked = false };
                cardCollectionData.Cards.Add(cardData);
            }

            var storage = await GetCardService();
            await storage.SaveAsync(cardCollectionData);
            Debug.LogWarning($"Debug Save Completed");
        }
        
        private async UniTask Load()
        {
            var storage = await GetCardService();
            var saveData = await storage.LoadAsync(TestEventId);

            Debug.LogWarning($"Debug saveData {saveData.EventId} / {saveData.Cards.Count}");
            foreach (var card in saveData.Cards)
            {
                Debug.LogWarning($"Debug card {card.CardId} / {card.IsUnlocked}");
            }
        }

        private async UniTask Clear()
        {
            var storage = await GetCardService();
            await storage.ClearCollectionAsync();
            Debug.LogWarning($"Debug Clear Completed");
        }

        private async UniTask<EventCardsService> GetCardService()
        {
            if (_eventCardService != null) return _eventCardService;
            
            _eventCardService = new EventCardsService(new JsonEventCardsStorage());
            
            await _eventCardService.InitializeAsync();

            return _eventCardService;
        }
        
        private void OnDestroy()
        {
            _saveButton.onClick.RemoveAllListeners();
            _loadButton.onClick.RemoveAllListeners();
            _clearButton.onClick.RemoveAllListeners();
        }

        public async UniTask OpenCard(string cardId)
        {
            var storage = await GetCardService();
            await storage.UnlockCardAsync(TestEventId, cardId);

        }
    }
}