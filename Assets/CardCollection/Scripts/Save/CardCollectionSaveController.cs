using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class CardCollectionSaveController : MonoBehaviour
    {
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _loadButton;
        
        private const string TestEventId = "test";
        
        private IEventCardsStorage _eventCardsStorage;

        private void Start()
        {
            _saveButton.onClick.AddListener(() => Save().Forget());
            _loadButton.onClick.AddListener(() => Load().Forget());
        }

        private async UniTask<IEventCardsStorage> GetCardStorage()
        {
            if (_eventCardsStorage != null) return _eventCardsStorage;
            
            _eventCardsStorage = new ServerEventCardsStorage();
            await _eventCardsStorage.InitializeAsync();

            return _eventCardsStorage;
        }
        
        private async UniTask Save()
        {
            var cardCollectionData = new EventCardsSaveData{ EventId = TestEventId };
            
            var card1 = new CardProgressData { CardId = "1", IsUnlocked = false };
            cardCollectionData.Cards.Add(card1);
            
            var card2 = new CardProgressData { CardId = "2", IsUnlocked = false };
            cardCollectionData.Cards.Add(card2);
            
            var card3 = new CardProgressData { CardId = "3", IsUnlocked = true };
            cardCollectionData.Cards.Add(card3);

            var storage = await GetCardStorage();
            await storage.SaveAsync(cardCollectionData);
            Debug.LogWarning($"Debug Save Completed");
        }
        
        private async UniTask Load()
        {
            var storage = await GetCardStorage();
            var saveData = await storage.LoadAsync(TestEventId);

            Debug.LogWarning($"Debug saveData {saveData.EventId} / {saveData.Cards.Count}");
            foreach (var card in saveData.Cards)
            {
                Debug.LogWarning($"Debug card {card.CardId} / {card.IsUnlocked}");
            }
        }
        
        private void OnDestroy()
        {
            _saveButton.onClick.RemoveAllListeners();
            _loadButton.onClick.RemoveAllListeners();
        }
    }
}