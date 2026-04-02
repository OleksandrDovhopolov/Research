using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionFeatureFacadeFacade : ICardCollectionFeatureFacade, IDisposable
    {
        public bool IsActive { get; private set; }
        public CardCollectionSessionContext FeatureContext { get; private set; }
        
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

        public bool TryGetCollectionModule(out ICardCollectionModule module)
        {
            module = FeatureContext?.Module;
            return module != null;
        }

        public bool TryGetCollectionPointsAccount(out ICardCollectionPointsAccount pointsAccount)
        {
            pointsAccount = FeatureContext?.PointsAccount;
            return pointsAccount != null;
        }

        public UniTask ShowNewCardWindow(string packId, CancellationToken ct)
        {
            if (FeatureContext == null)
            {
                Debug.LogWarning($"[CardCollectionRuntime] ShowNewCardWindow skipped for {packId}: session context is null.");
                return UniTask.CompletedTask;
            }

            Debug.LogWarning($"[CardCollectionRuntime] ShowNewCardWindow {packId}");
            
            FeatureContext.WindowOpener.OpenNewCardWindow(packId);
            return UniTask.CompletedTask;
        }
        
        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}