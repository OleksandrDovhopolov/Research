using Cysharp.Threading.Tasks;
using UISystem;
using VContainer;

namespace CardCollectionImpl
{
    public class CollectionCompletedArgs : WindowArgs
    {
        public string EventId;
        public string EventName;
        
        public CollectionCompletedArgs(string eventId, string eventName)
        {
            EventId = eventId;
            EventName = eventName;
        }
    }
    
    [Window("CollectionCompletedWindow")]
    public class CollectionCompletedController :  WindowController<CollectionCompletedView>
    {
        private CollectionCompletedArgs Args => (CollectionCompletedArgs) Arguments;
        
        [Inject]
        private void Construct()
        {
        }
        
        protected override void OnShowStart()
        {
            View.SetDescription(Args.EventName);
            View.LoadCollectionSprite(Args.EventId).Forget();
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
        }
        
        private void CloseWindow()
        {
            UIManager.Hide<CollectionCompletedController>();
        }
    }
}