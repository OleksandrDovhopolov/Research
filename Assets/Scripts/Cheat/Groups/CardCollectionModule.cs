using System.Collections.Generic;
using System.Threading;
using CardCollectionImpl;
using cheatModule;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class CardCollectionModule : ICheatsModule
    {
        private const string CardCollectionPointsGroup = "CardCollectionPointsGroup";
        
        private readonly ICardCollectionFeatureFacade _featureFacade;
        private readonly CancellationToken _ct;
        
        public CardCollectionModule(ICardCollectionFeatureFacade featureFacade, CancellationToken ct)
        {
            _featureFacade = featureFacade;
            _ct = ct;
        }

        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Save collection", () =>
            {
                if (_featureFacade.TryGetCollectionUpdater(out var updater))
                {
                    updater.Save(_ct).Forget();
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection updater is unavailable.");
                }
            }));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Load collection", () =>
            {
                if (_featureFacade.TryGetCollectionReader(out var reader))
                {
                    reader.Load(_ct).Forget();
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection reader is unavailable.");
                }
            }));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Clear collection", () =>
            {
                if (_featureFacade.TryGetCollectionUpdater(out var updater))
                {
                    updater.Clear(_ct).Forget();
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection updater is unavailable.");
                }
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
                if (_featureFacade.TryGetCollectionUpdater(out var updater))
                {
                    updater.UnlockCard(cardId, _ct).Forget();
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection updater is unavailable.");
                }
            }));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add points(int)", points =>
            {
                if (_featureFacade.TryGetCollectionPointsAccount(out var pointsAccount))
                {
                    pointsAccount.TryAddPointsAsync(points, _ct).Forget();
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection points account is unavailable.");
                }
            }).WithGroup(CardCollectionPointsGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove points(int)", points =>
            {
                if (_featureFacade.TryGetCollectionPointsAccount(out var pointsAccount))
                {
                    pointsAccount.TrySpendPointsAsync(points, _ct).Forget();
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection points account is unavailable.");
                }
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
                if (_featureFacade.TryGetCollectionUpdater(out var updater))
                {
                    await updater.UnlockCard(cardId, ct);
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection updater is unavailable.");
                    return;
                }
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
                if (_featureFacade.TryGetCollectionUpdater(out var updater))
                {
                    await updater.UnlockCard(cardIds[i], ct);
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection updater is unavailable.");
                    return;
                }
            }
        }

        private async UniTask<List<string>> GetAllCardIdsAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!_featureFacade.TryGetCollectionReader(out var reader))
            {
                Debug.LogWarning("[Cheat] CardCollection reader is unavailable.");
                return new List<string>();
            }

            var data = await reader.Load(ct);

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
