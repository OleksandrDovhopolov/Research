using CoreResources;
using UISystem;
using UnityEngine;

namespace UIShared
{
    public class GameplaySceneView : WindowView
    {
        [SerializeField] private ResourcesView _resourcesView;
        [SerializeField] private EventDebugView _debugView;
        
        public void AddUpcomingEvent(string eventId, string spriteAddress, IGlobalTimerService globalTimerService)
        {
            _debugView.AddUpcoming(eventId, spriteAddress, globalTimerService);
        }

        public void RemoveEventById(string eventId)
        {
            _debugView.OnEventStarted(eventId);
        }
    }
}