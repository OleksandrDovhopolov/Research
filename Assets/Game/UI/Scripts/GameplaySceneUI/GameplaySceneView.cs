using EventOrchestration;
using UISystem;
using UnityEngine;

namespace GameplayUI
{
    public class GameplaySceneView : WindowView
    {
        [SerializeField] private ResourcesView _resourcesView;
        [SerializeField] private EventDebugView _debugView;
        
        [Space, Header("Main Menu")]
        [SerializeField] private EventButton _cardCollectionButton;

        protected override void Awake()
        {
            base.Awake();
            _cardCollectionButton.gameObject.SetActive(false);
        }
        
        public void AddUpcomingEvent(string eventId, string spriteAddress, IGlobalTimerService globalTimerService)
        {
            _debugView.AddUpcoming(eventId, spriteAddress, globalTimerService);
        }

        public void RemoveEventById(string eventId)
        {
            _debugView.OnEventStarted(eventId);
        }
        
        public IEventButton GetEventButton()
        {
            return _cardCollectionButton;
        }
    }
}