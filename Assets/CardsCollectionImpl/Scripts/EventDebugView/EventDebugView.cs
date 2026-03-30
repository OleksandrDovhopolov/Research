using System;
using System.Collections.Generic;
using System.Threading;
using CardCollectionImpl;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;
using VContainer;

namespace UIShared
{
    public class EventDebugView : MonoBehaviour
    {
        private const string CollectionPreview = "Collection_preview";
        
        [SerializeField] private UIListPool<EventDebugItemView> _uiListPool;
        [SerializeField] private List<EventData> _eventData;

        private readonly Dictionary<string, EventDebugItemView> _idToView = new();

        private CancellationToken _ct;
        /*private IEventSpriteManager  _eventSpriteManager;
        
        [Inject]
        private void Construct(IEventSpriteManager eventSpriteManager)
        {
            _eventSpriteManager = eventSpriteManager;
        }*/
        
        private void Start()
        {
            if (_uiListPool == null)
            {
                Debug.LogWarning("[EventDebugView] Assign _uiListPool in the inspector.");
                return;
            }

            _ct = this.GetCancellationTokenOnDestroy();
            _uiListPool.DisableAll();
        }

        public void AddUpcoming(string eventId, IGlobalTimerService  globalTimerService)
        {
            if (_uiListPool == null) return;
            if (string.IsNullOrEmpty(eventId)) return;
            if (_idToView.ContainsKey(eventId)) return;

            SetDataAsync(eventId, globalTimerService).Forget();
        }

        private async UniTask SetDataAsync(string eventId, IGlobalTimerService  globalTimerService)
        {
            var view = _uiListPool.GetNext();
            var spriteAddress = eventId + "/" + CollectionPreview;
            var sprite = await LoadCollectionSprite(spriteAddress);
            //await _eventSpriteManager.BindSpriteAsync(eventId, CollectionPreview, view.Image, _ct);
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
            
            var spriteAddress = eventId + "/" + CollectionPreview;
            ProdAddressablesWrapper.Release(spriteAddress);
            _uiListPool.DisableNonActive();
        }
    }
}