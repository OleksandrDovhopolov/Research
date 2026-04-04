using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionSessionFacade : ICardCollectionSessionFacade, IDisposable
    {
        private CardCollectionSessionContext _featureContext;
        
        public bool IsActive { get; private set; }
        
        void ICardCollectionSessionFacade.SetActiveSession(CardCollectionSessionContext sessionContext)
        {
            _featureContext = sessionContext ?? throw new ArgumentNullException(nameof(sessionContext));
            Debug.LogWarning($"[CardCollectionRuntime] SetActiveSession");
            IsActive = true;
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
            ct.ThrowIfCancellationRequested();

            if (_featureContext == null)
            {
                Debug.LogWarning($"[CardCollectionRuntime] ShowNewCardWindow skipped for {packId}: session context is null.");
                return UniTask.CompletedTask;
            }

            Debug.LogWarning($"[CardCollectionRuntime] ShowNewCardWindow {packId}");

            var module = _featureContext.Module;
            var pointsAccount = _featureContext.PointsAccount;
            if (module.GetPackById(packId) == null)
            {
                Debug.LogError($"Failed to find pack with id {packId}");
                return UniTask.CompletedTask;
            }

            var args = new NewCardArgs(module.EventId, packId, module, pointsAccount);
            _featureContext.WindowCoordinator.ShowNewCard(args);
            return UniTask.CompletedTask;
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