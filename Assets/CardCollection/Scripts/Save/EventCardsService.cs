using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace core
{
    public class EventCardsService
    {
        private readonly IEventCardsStorage _storage;

        public EventCardsService(IEventCardsStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async UniTask InitializeAsync()
        {
            await _storage.InitializeAsync();
        }

        public UniTask<EventCardsSaveData> LoadAsync(string eventId)
        {
            return _storage.LoadAsync(eventId);
        }

        public UniTask SaveAsync(EventCardsSaveData data)
        {
            return _storage.SaveAsync(data);
        }

        public UniTask ClearCollectionAsync()
        {
            return _storage.ClearCollectionAsync();
        }
        
        public UniTask UnlockCardAsync(string eventId, string cardId)
        {
            return UnlockCardsAsync(eventId, new[] { cardId });
        }

        public async UniTask UnlockCardsAsync(string eventId, IReadOnlyCollection<string> cardIds)
        {
            if (cardIds == null || cardIds.Count == 0) return;

            var data = await _storage.LoadAsync(eventId);

            foreach (var cardId in cardIds)
            {
                var card = data.Cards.Find(c => c.CardId == cardId);

                if (card == null)
                {
                    data.Cards.Add(new CardProgressData { CardId = cardId, IsUnlocked = true });
                }
                else
                {
                    card.IsUnlocked = true;
                }
            }

            await _storage.SaveAsync(data);
        }
        
        public async UniTask<bool> IsCardUnlockedAsync(string eventId, string cardId)
        {
            var data = await _storage.LoadAsync(eventId);
            var card = data.Cards.Find(c => c.CardId == cardId);
            return card != null && card.IsUnlocked;
        }
    }
}