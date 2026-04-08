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
        public Sprite Sprite;
        
        public CollectionStartedArgs(string eventId, string eventName, Sprite previewSprite)
        {
            EventId = eventId;
            EventName = eventName;
            Sprite = previewSprite;
        }
    }

    [Window("CollectionStartedWindow")]
    public class CollectionStartedController :  WindowController<CollectionStartedView>
    {
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
            View.SetCollectionImage(Args.Sprite);
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

        protected override void OnHideComplete(bool isClosed)
        {
            View.Release();
        }

        private void CloseWindow()
        {
            UIManager.Hide<CollectionStartedController>();
        }
    }
}