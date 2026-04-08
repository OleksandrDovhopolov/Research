using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UIShared;
using UnityEngine;

namespace HUD
{
    public class EventViewData
    {
        public string EventId;
        public string SpriteAddress;
        public EventDebugItemView View;
    }
    
    public class EventDebugView : MonoBehaviour
    {
        [SerializeField] private UIListPool<EventDebugItemView> _uiListPool;

        private readonly List<EventViewData> _idToView = new();

        private CancellationToken _ct;
        
        private readonly SemaphoreSlim _addUpcomingSemaphore = new(1, 1);
        
        private void Start()
        {
            if (_uiListPool == null)
            {
                Debug.LogWarning("[EventDebugView] Assign _uiListPool in the inspector.");
                return;
            }

            _ct = this.GetCancellationTokenOnDestroy();
        }

        public void AddUpcoming(string eventId, string spriteAddress, IGlobalTimerService  globalTimerService)
        {
            Debug.LogWarning($"[Debug] state.State  AddUpcoming eventId {eventId}");
            if (_uiListPool == null) return;
            if (string.IsNullOrEmpty(eventId)) return;
            if (_idToView.Find(data => data.EventId == eventId) != null) return;

            SetDataAsync(eventId, spriteAddress, globalTimerService, _ct).Forget();
        }

        private async UniTask SetDataAsync(string eventId, string spriteAddress, IGlobalTimerService  globalTimerService, CancellationToken ct)
        {
            await _addUpcomingSemaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();

                if (_idToView.Find(data => data.EventId == eventId) != null) return;

                var sprite = await LoadCollectionSprite(spriteAddress, ct);
                if (sprite == null) return;

                var view = _uiListPool.GetNext();
                view.SetData(eventId, sprite, globalTimerService);

                _idToView.Add(new EventViewData
                {
                    EventId = eventId,
                    SpriteAddress = spriteAddress,
                    View = view
                });
            }
            finally
            {
                _addUpcomingSemaphore.Release();
            }
        }
        
        public async UniTask<Sprite> LoadCollectionSprite(string spriteAddress, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                return await ProdAddressablesWrapper.LoadAsync<Sprite>(spriteAddress, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Debug.LogWarning($"Failed to load sprite for EventId='{spriteAddress}'. {ex.Message}");
                return null;
            }
        }
        
        public void OnEventStarted(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || _uiListPool == null) return;

            var viewData = _idToView.Find(data => data.EventId == eventId);
            if (viewData != null)
            {
                viewData.View.ResetSprite();
                _uiListPool.Return(viewData.View);
                _idToView.Remove(viewData);
                ProdAddressablesWrapper.Release(viewData.SpriteAddress);
            }
            
            _uiListPool.DisableNonActive();
        }
    }
}