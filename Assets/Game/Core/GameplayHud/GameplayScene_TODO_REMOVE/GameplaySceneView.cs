using CoreResources;
using EventOrchestration;
using UIShared;
using UISystem;
using UnityEngine;

namespace HUD
{
    public class GameplaySceneView : WindowView
    {
        [SerializeField] private ResourcesView _resourcesView;
        [SerializeField] private EventDebugView _debugView;
        [SerializeField] private RectTransform _eventButtonContainer;
        
        public void AddUpcomingEvent(string eventId, string spriteAddress, IGlobalTimerService globalTimerService)
        {
            _debugView.AddUpcoming(eventId, spriteAddress, globalTimerService);
        }

        public void RemoveEventById(string eventId)
        {
            _debugView.OnEventStarted(eventId);
        }

        public RectTransform GetButtonContainer()
        {
            return _eventButtonContainer;
        }
    }
}