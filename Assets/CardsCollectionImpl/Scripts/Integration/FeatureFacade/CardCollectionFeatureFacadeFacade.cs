using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionFeatureFacadeFacade : ICardCollectionFeatureFacade, IDisposable
    {
        private CardCollectionSessionContext _featureContext;
        
        public bool IsActive { get; private set; }
        
        public void SetActiveSession(CardCollectionSessionContext sessionContext)
        {
            _featureContext = sessionContext ?? throw new ArgumentNullException(nameof(sessionContext));
            Debug.LogWarning($"[CardCollectionRuntime] SetActiveSession");
            IsActive = true;
        }

        public void ClearSession()
        {
            Debug.LogWarning($"[CardCollectionRuntime] ClearSession");
            Dispose();
        }

        public bool TryGetCollectionModule(out ICardCollectionModule module)
        {
            module = _featureContext?.Module;
            return module != null;
        }

        public bool TryGetCollectionPointsAccount(out ICardCollectionPointsAccount pointsAccount)
        {
            pointsAccount = _featureContext?.PointsAccount;
            return pointsAccount != null;
        }

        public UniTask ShowNewCardWindow(string packId, CancellationToken ct)
        {
            if (_featureContext == null)
            {
                Debug.LogWarning($"[CardCollectionRuntime] ShowNewCardWindow skipped for {packId}: session context is null.");
                return UniTask.CompletedTask;
            }

            Debug.LogWarning($"[CardCollectionRuntime] ShowNewCardWindow {packId}");
            
            _featureContext.WindowOpener.OpenNewCardWindow(packId);
            return UniTask.CompletedTask;
        }
        
        public void Dispose()
        {
            _featureContext = null;
            IsActive = false;
        }
    }
}