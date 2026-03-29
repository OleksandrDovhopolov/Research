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
        private const string CollectionBackground = "Collection_background";
        
        private CollectionCompletedArgs Args => (CollectionCompletedArgs) Arguments;
        
        [Inject]
        private void Construct()
        {
        }
        
        protected override void OnShowStart()
        {
            View.SetDescription(Args.EventName);
            var collectionBackground = Args.EventId + "/" + CollectionBackground;
            View.LoadCollectionSprite(collectionBackground).Forget();
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