using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public sealed class CollectionProgressQueryService : ICollectionProgressQueryService
    {
        private readonly CardProgressService _cardProgressService;

        public CollectionProgressQueryService(CardProgressService cardProgressService)
        {
            _cardProgressService = cardProgressService ?? throw new ArgumentNullException(nameof(cardProgressService));
        }

        public UniTask<EventCardsSaveData> LoadAsync(string eventId, CancellationToken ct = default)
        {
            return _cardProgressService.LoadAsync(eventId, ct);
        }

        public UniTask<List<CardProgressData>> GetCardsByIdsAsync(string eventId, List<string> cardIds, CancellationToken ct = default)
        {
            return _cardProgressService.GetCardsByIdsAsync(eventId, cardIds, ct);
        }
    }
}
