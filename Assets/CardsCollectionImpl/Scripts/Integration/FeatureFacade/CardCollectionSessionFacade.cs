using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionSessionFacade : ICardCollectionSessionFacade, IDisposable
    {
        private CardCollectionSessionContext _featureContext;
        
        private bool IsActive { get; set; }
        
        void ICardCollectionSessionFacade.SetActiveSession(CardCollectionSessionContext sessionContext)
        {
            _featureContext = sessionContext ?? throw new ArgumentNullException(nameof(sessionContext));
            Debug.LogWarning($"[CardCollectionRuntime] SetActiveSession");
            IsActive = true;
        }

        public async UniTask TryOpenPackById(string packId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!TryGetActiveContext(out var context))
            {
                Debug.LogWarning($"[CardCollectionRuntime] TryShowNewCardWindow skipped for {packId}: session context is null.");
                return;
            }
            
            await context.OpenPackFlow.OpenPackById(packId, ct);
        }

        public async UniTask TryUnlockCards(IReadOnlyCollection<string> cardIds, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!TryGetActiveContext(out var context))
            {
                Debug.LogWarning("[CardCollectionRuntime] TryUnlockCards skipped: session context is null.");
                return;
            }

            if (cardIds == null || cardIds.Count == 0)
            {
                Debug.LogWarning("[CardCollectionRuntime] TryUnlockCards skipped: cardIds is null or empty.");
                return;
            }

            await context.CardCollectionFacade.UnlockCards(cardIds, ct);
        }

        public async UniTask TryAddPoints(int points, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!TryGetActiveContext(out var context))
            {
                Debug.LogWarning("[CardCollectionRuntime] TryAddPoints skipped: session context is null.");
                return;
            }

            await context.CardCollectionFacade.TryAddPointsAsync(points, ct);
        }

        public async UniTask TryRemovePoints(int points, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!TryGetActiveContext(out var context))
            {
                Debug.LogWarning("[CardCollectionRuntime] TryRemovePoints skipped: session context is null.");
                return;
            }

            await context.CardCollectionFacade.TrySpendPointsAsync(points, ct);
        }

        public async UniTask TryCompleteAllCollection(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!TryGetActiveContext(out var context))
            {
                Debug.LogWarning("[CardCollectionRuntime] TryCompleteAllCollection skipped: session context is null.");
                return;
            }

            var cardIds = await GetAllCardIdsAsync(context, ct);
            if (cardIds.Count == 0)
            {
                Debug.LogWarning("[CardCollectionRuntime] Could not find card IDs to complete collection.");
                return;
            }

            foreach (var cardId in cardIds)
            {
                ct.ThrowIfCancellationRequested();
                await context.CardCollectionFacade.UnlockCards(new[] { cardId }, ct);
            }
        }

        public async UniTask TryUnlockAllMinusOneCard(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!TryGetActiveContext(out var context))
            {
                Debug.LogWarning("[CardCollectionRuntime] TryUnlockAllMinusOneCard skipped: session context is null.");
                return;
            }

            var cardIds = await GetAllCardIdsAsync(context, ct);
            if (cardIds.Count <= 1)
            {
                Debug.LogWarning("[CardCollectionRuntime] Not enough cards to unlock all minus one.");
                return;
            }

            var unlockIds = cardIds.Take(cardIds.Count - 1).ToList();
            await context.CardCollectionFacade.UnlockCards(unlockIds, ct);
        }

        public async UniTask TryUnlockGroupByIndex(int groupIndex, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!TryGetActiveContext(out var context))
            {
                Debug.LogWarning("[CardCollectionRuntime] TryUnlockGroupByIndex skipped: session context is null.");
                return;
            }

            if (groupIndex < 0)
            {
                Debug.LogWarning("[CardCollectionRuntime] Group index must be >= 0.");
                return;
            }

            var cardIds = await GetAllCardIdsAsync(context, ct);
            if (cardIds.Count == 0)
            {
                Debug.LogWarning("[CardCollectionRuntime] Could not find card IDs to unlock group.");
                return;
            }

            const int groupSize = 10;
            var groupCardIds = cardIds
                .Skip(groupIndex * groupSize)
                .Take(groupSize)
                .ToList();

            if (groupCardIds.Count == 0)
            {
                Debug.LogWarning($"[CardCollectionRuntime] Group index {groupIndex} is out of range.");
                return;
            }

            foreach (var cardId in groupCardIds)
            {
                ct.ThrowIfCancellationRequested();
                await context.CardCollectionFacade.UnlockCards(new[] { cardId }, ct);
            }
        }

        private static async UniTask<List<string>> GetAllCardIdsAsync(CardCollectionSessionContext context, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var data = await context.CardCollectionFacade.Load(ct);
            var result = new List<string>();
            var seen = new HashSet<string>();
            foreach (var card in data.Cards)
            {
                ct.ThrowIfCancellationRequested();
                if (!string.IsNullOrEmpty(card?.CardId) && seen.Add(card.CardId))
                {
                    result.Add(card.CardId);
                }
            }

            return result;
        }

        private bool TryGetActiveContext(out CardCollectionSessionContext context)
        {
            context = _featureContext;
            return context != null && IsActive;
        }

        void ICardCollectionSessionFacade.ClearSession()
        {
            Debug.LogWarning($"[CardCollectionRuntime] ClearSession");
            Dispose();
        }


        public void Dispose()
        {
            _featureContext = null;
            IsActive = false;
        }
    }
}