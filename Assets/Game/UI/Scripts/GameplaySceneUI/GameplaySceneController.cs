using EventOrchestration;
using UISystem;
using VContainer;

namespace GameplayUI
{
    [Window("GameplaySceneController", isRoot: true)]
    public class GameplaySceneController : WindowController<GameplaySceneView>
    {
        [Inject]
        public void Install()
        {
        }

        protected override void OnShowStart()
        {
        }

        protected override void OnShowComplete()
        {
        }

        public void AddUpcomingEvent(string eventId, string spriteAddress, IGlobalTimerService globalTimerService)
        {
            View.AddUpcomingEvent(eventId, spriteAddress, globalTimerService);
        }

        public void RemoveEventById(string eventId)
        {
            View.RemoveEventById(eventId);
        }
        
        protected override void OnHideStart(bool isClosed)
        {
        }
        
        public IEventButton GetEventButton()
        {
            return View.GetEventButton();
        }
    }
}