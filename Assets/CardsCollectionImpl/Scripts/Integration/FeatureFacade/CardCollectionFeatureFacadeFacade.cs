using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionFeatureFacadeFacade : ICardCollectionFeatureFacade, IDisposable
    {
        private readonly UIManager _uiManager;

        public bool IsActive { get; private set; }
        public CardCollectionSessionContext FeatureContext { get; private set; }

        public CardCollectionFeatureFacadeFacade(UIManager uiManager)
        {
            _uiManager = uiManager;
        }
        
        public void SetActiveSession(CardCollectionSessionContext sessionContext)
        {
            FeatureContext = sessionContext ?? throw new ArgumentNullException(nameof(sessionContext));
            Debug.LogWarning($"[CardCollectionRuntime] SetActiveSession");
            IsActive = true;
        }

        public void ClearSession()
        {
            Debug.LogWarning($"[CardCollectionRuntime] ClearSession");
            FeatureContext = null;
            IsActive = false;
        }

        public bool TryGetCollectionUpdater(out ICardCollectionUpdater updater)
        {
            updater = FeatureContext?.Updater;
            return updater != null;
        }

        public bool TryGetCollectionReader(out ICardCollectionReader reader)
        {
            reader = FeatureContext?.Reader;
            return reader != null;
        }

        public bool TryGetCollectionPointsAccount(out ICardCollectionPointsAccount pointsAccount)
        {
            pointsAccount = FeatureContext?.PointsAccount;
            return pointsAccount != null;
        }

        public UniTask ShowCardCollectionWindow(CancellationToken ct)
        {
            if (FeatureContext == null)
            {
                Debug.LogWarning("[CardCollectionRuntime] ShowCardCollectionWindow skipped: session context is null.");
                return UniTask.CompletedTask;
            }

            Debug.LogWarning($"[CardCollectionRuntime] ShowCardCollectionWindow");
            return UniTask.CompletedTask;
        }

        public UniTask ShowNewCardWindow(string packId, CancellationToken ct)
        {
            if (FeatureContext == null)
            {
                Debug.LogWarning($"[CardCollectionRuntime] ShowNewCardWindow skipped for {packId}: session context is null.");
                return UniTask.CompletedTask;
            }

            Debug.LogWarning($"[CardCollectionRuntime] ShowNewCardWindow {packId}");
            return UniTask.CompletedTask;
        }
        
        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}