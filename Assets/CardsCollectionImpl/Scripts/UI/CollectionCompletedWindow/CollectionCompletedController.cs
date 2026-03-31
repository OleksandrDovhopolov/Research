using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UISystem;
using UnityEngine;
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
        
        private IEventSpriteManager  _eventSpriteManager;
        private CollectionCompletedArgs Args => (CollectionCompletedArgs) Arguments;
        
        [Inject]
        private void Construct(IEventSpriteManager eventSpriteManager)
        {
            _eventSpriteManager = eventSpriteManager;
        }
        
        protected override void OnShowStart()
        {
            View.SetDescription(Args.EventName);
            var spriteAddress = Args.EventId + "/" + CollectionBackground;
            View.LoadCollectionSprite(spriteAddress).Forget();
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
        }
        
        protected override void OnHideComplete(bool isClosed)
        {
            View.Release();
        }
        
        private void CloseWindow()
        {
            UIManager.Hide<CollectionCompletedController>();
        }
    }
}