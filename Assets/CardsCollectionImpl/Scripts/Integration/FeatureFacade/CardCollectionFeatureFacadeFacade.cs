using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionFeatureFacadeFacade : ICardCollectionFeatureFacade, IDisposable
    {
        private readonly UIManager _uiManager;

        public bool IsActive { get; private set; }

        public CardCollectionFeatureFacadeFacade(UIManager uiManager)
        {
            _uiManager = uiManager;
        }
        
        public void SetActiveSession()
        {
            Debug.LogWarning($"[CardCollectionRuntime] SetActiveSession");
            IsActive = true;
        }

        public void ClearSession()
        {
            Debug.LogWarning($"[CardCollectionRuntime] ClearSession");
            IsActive = false;
        }

        public UniTask ShowCardCollectionWindow(CancellationToken ct)
        {
            Debug.LogWarning($"[CardCollectionRuntime] ShowCardCollectionWindow");
            return UniTask.CompletedTask;
        }

        public UniTask ShowNewCardWindow(string packId, CancellationToken ct)
        {
            Debug.LogWarning($"[CardCollectionRuntime] ShowNewCardWindow {packId}");
            return UniTask.CompletedTask;
        }
        
        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}