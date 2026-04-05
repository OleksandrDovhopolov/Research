using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;

namespace UIShared
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

        //private readonly Dictionary<string, EventDebugItemView> _idToView = new();
        private readonly List<EventViewData> _idToView = new();

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

        public void AddUpcoming(string eventId, string spriteAddress, IGlobalTimerService  globalTimerService)
        {
            Debug.LogWarning($"[Debug] AddUpcoming eventId {eventId}");
            if (_uiListPool == null) return;
            if (string.IsNullOrEmpty(eventId)) return;
            if (_idToView.Find(data => data.EventId == eventId) != null) return;

            SetDataAsync(eventId, spriteAddress, globalTimerService).Forget();
        }

        private async UniTask SetDataAsync(string eventId, string spriteAddress, IGlobalTimerService  globalTimerService)
        {
            var sprite = await LoadCollectionSprite(spriteAddress);
            
            var view = _uiListPool.GetNext();
            view.SetData(eventId, sprite, globalTimerService);
            var data = new EventViewData
            {
                EventId = eventId,
                SpriteAddress = spriteAddress,
                View = view
            };
            _idToView.Add(data);
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