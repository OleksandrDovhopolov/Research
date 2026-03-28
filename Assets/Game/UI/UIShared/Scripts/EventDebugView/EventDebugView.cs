using System.Collections.Generic;
using UnityEngine;

namespace UIShared
{
    public class EventDebugView : MonoBehaviour
    {
        [SerializeField] private UIListPool<EventDebugItemView> _uiListPool;
        [SerializeField] private List<EventData> _eventData;
        [SerializeField] private EventData _fallbackData;

        private readonly Dictionary<string, EventDebugItemView> _idToView = new();

        private void Start()
        {
            if (_uiListPool == null)
            {
                Debug.LogWarning("[EventDebugView] Assign _uiListPool in the inspector.");
                return;
            }

            _uiListPool.DisableAll();
        }

        public void AddUpcoming(string eventId, IGlobalTimerService  globalTimerService)
        {
            if (_uiListPool == null) return;
            if (string.IsNullOrEmpty(eventId)) return;
            if (_idToView.ContainsKey(eventId)) return;

            var view = _uiListPool.GetNext();
            view.SetData(eventId, GetEventIcon(eventId), globalTimerService);
            _idToView[eventId] = view;
        }

        private Sprite GetEventIcon(string eventId)
        {
            var eventData = _eventData.Find(data => data.EventId == eventId);
            if (eventData == null)
            {
                Debug.LogWarning($"[EventDebugView] EventData not found: {eventId}. Default used");
                eventData = _fallbackData;
            }
            
            return eventData.EventIcon;
        }
        
        public void OnEventStarted(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || _uiListPool == null) return;

            if (_idToView.TryGetValue(eventId, out var view) && view != null)
            {
                _uiListPool.Return(view);
                _idToView.Remove(eventId);
            }
            
            _uiListPool.DisableNonActive();
        }
    }
}