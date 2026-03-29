using Cysharp.Threading.Tasks;
using UIShared;
using UISystem;
using UnityEngine;
using VContainer;

namespace CardCollectionImpl
{
    public class CollectionStartedArgs : WindowArgs
    {
        public string EventId;
        public string EventName;
        
        public CollectionStartedArgs(string eventId, string eventName)
        {
            EventId = eventId;
            EventName = eventName;
        }
    }

    [Window("CollectionStartedWindow")]
    public class CollectionStartedController :  WindowController<CollectionStartedView>
    {
        private const string CollectionBackground = "Collection_background";
        
        private IGlobalTimerService _globalTimerService;
        
        private CollectionStartedArgs Args => (CollectionStartedArgs) Arguments;
        
        [Inject]
        private void Construct(IGlobalTimerService globalTimerService)
        {
            _globalTimerService  = globalTimerService;
        }

        protected override void OnShowStart()
        {
            View.SetTimer(Args.EventId, _globalTimerService);
            View.SetDescription(Args.EventName);
            var collectionBackground = Args.EventId + "/" + CollectionBackground;
            Debug.LogWarning($"[Debug] rgs.EventId {Args.EventId}, collectionBackground {collectionBackground}");
            View.LoadCollectionSprite(collectionBackground).Forget();
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.RemoveTimer();
            View.CloseClick -= CloseWindow;
        }
        
        private void CloseWindow()
        {
            UIManager.Hide<CollectionStartedController>();
        }
    }
}