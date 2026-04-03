using System;
using System.Collections.Generic;
using System.Threading;
using CardCollectionImpl;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;

namespace UIShared
{
    public class EventDebugView : MonoBehaviour
    {
        [SerializeField] private UIListPool<EventDebugItemView> _uiListPool;

        private readonly Dictionary<string, EventDebugItemView> _idToView = new();

        private CancellationToken _ct;
        
        private void Start()
        {
            if (_uiListPool == null)
            {
                Debug.LogWarning("[EventDebugView] Assign _uiListPool in the inspector.");
                return;
            }

            _ct = this.GetCancellationTokenOnDestroy();
        }

        public void AddUpcoming(string eventId, IGlobalTimerService  globalTimerService)
        {
            Debug.LogWarning($"[Debug] AddUpcoming eventId {eventId}");
            if (_uiListPool == null) return;
            if (string.IsNullOrEmpty(eventId)) return;
            if (_idToView.ContainsKey(eventId)) return;

            SetDataAsync(eventId, globalTimerService).Forget();
        }

        private async UniTask SetDataAsync(string eventId, IGlobalTimerService  globalTimerService)
        {
            var sprite = await LoadCollectionSprite(GetSpriteAddress(eventId));
            
            var view = _uiListPool.GetNext();
            view.SetData(eventId, sprite, globalTimerService);
            _idToView[eventId] = view;
        }
        
        public async UniTask<Sprite> LoadCollectionSprite(string spriteAddress)
        {
            _ct.ThrowIfCancellationRequested();
            
            Sprite sprite = null;
            try
            {
                sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(spriteAddress, _ct);
            }
            catch (Exception loadPrimaryException)
            {
                Debug.LogWarning($"Failed to load sprite for EventId='{spriteAddress}'. Falling back to default. {loadPrimaryException.Message}");
            }

            return sprite;
        }
        
        public void OnEventStarted(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || _uiListPool == null) return;

            if (_idToView.TryGetValue(eventId, out var view) && view != null)
            {
                view.ResetSprite();
                _uiListPool.Return(view);
                _idToView.Remove(eventId);
            }
            
            ProdAddressablesWrapper.Release(GetSpriteAddress(eventId));
            _uiListPool.DisableNonActive();
        }

        private string GetSpriteAddress(string eventId)
        {
            return eventId + "/" + CardCollectionGeneralConfig.CollectionPreview;
        }
    }
}