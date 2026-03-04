using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using cheatModule;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class DefaultModule : ICheatsModule
    {
        private const string CardCollectionPointsGroup = "CardCollectionPointsGroup";
        
        private readonly ICardCollectionUpdater _collectionUpdater;
        private readonly ICardCollectionReader _cardCollectionReader;
        private readonly ICardCollectionPointsAccount _cardCollectionPointsAccount;
        private readonly CancellationToken _ct;
        
        public DefaultModule(
            ICardCollectionUpdater collectionUpdater,
            ICardCollectionReader cardCollectionReader,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            CancellationToken ct)
        {
            _collectionUpdater = collectionUpdater;
            _cardCollectionReader = cardCollectionReader;
            _cardCollectionPointsAccount = cardCollectionPointsAccount;
            _ct = ct;
        }
        
        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Save collection", () =>
            {
                _collectionUpdater.Save(_ct).Forget();
            }));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Load collection", () =>
            {
                _cardCollectionReader.Load(_ct).Forget();
            }));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Clear collection", () =>
            {
                _collectionUpdater.Clear(_ct).Forget();
            }));

            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Complete all collection", () =>
            {
                CompleteAllCollectionAsync(_ct).Forget();
            }));

            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Unlock all cards - 1", () =>
            {
                UnlockAllMinusOneCardAsync(_ct).Forget();
            }));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<string>("Open card ID(str)", cardId =>
            {
                _collectionUpdater.UnlockCard(cardId, _ct).Forget();
            }));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add points(int)", points =>
            {
                _cardCollectionPointsAccount.TryAddPointsAsync(points, _ct).Forget();
            }).WithGroup(CardCollectionPointsGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove points(int)", points =>
            {
                _cardCollectionPointsAccount.TrySpendPointsAsync(points, _ct).Forget();
            }).WithGroup(CardCollectionPointsGroup));
        }

        private async UniTask CompleteAllCollectionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var cardIds = await GetAllCardIdsAsync(ct);
            if (cardIds.Count == 0)
            {
                Debug.LogWarning("[Cheat] Could not find card IDs to complete collection.");
                return;
            }

            foreach (var cardId in cardIds)
            {
                ct.ThrowIfCancellationRequested();
                await _collectionUpdater.UnlockCard(cardId, ct);
            }
        }

        private async UniTask UnlockAllMinusOneCardAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var cardIds = await GetAllCardIdsAsync(ct);
            if (cardIds.Count <= 1)
            {
                Debug.LogWarning("[Cheat] Not enough cards to unlock all minus one.");
                return;
            }

            var unlockCount = cardIds.Count - 1;
            for (var i = 0; i < unlockCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                await _collectionUpdater.UnlockCard(cardIds[i], ct);
            }
        }

        private async UniTask<List<string>> GetAllCardIdsAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var data = await _cardCollectionReader.Load(ct);

            var result = new List<string>();
            var seen = new HashSet<string>();
            foreach (var card in data.Cards)
            {
                ct.ThrowIfCancellationRequested();
                if (!string.IsNullOrEmpty(card?.CardId) && seen.Add(card.CardId))
                    result.Add(card.CardId);
            }

            return result;
        }
    }
}
