using System;
using UISystem;

namespace CardCollectionImpl
{
    public sealed class CardCollectionWindowCoordinator : ICardCollectionWindowCoordinator
    {
        private readonly UIManager _uiManager;

        public CardCollectionWindowCoordinator(UIManager uiManager)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
        }

        public void ShowStarted(CollectionStartedArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            _uiManager.Show<CollectionStartedController>(args, UIShowCommand.UIShowType.Ordered);
        }

        public void ShowCompleted(CollectionCompletedArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            _uiManager.Show<CollectionCompletedController>(args, UIShowCommand.UIShowType.Ordered);
        }

        public void ShowCollection(CardCollectionArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            _uiManager.Show<CardCollectionController>(args, UIShowCommand.UIShowType.Ordered);
        }

        public void ShowNewCard(NewCardArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            _uiManager.Show<NewCardController>(args,UIShowCommand.UIShowType.Ordered);
        }

        public void ShowGroupCompleted(CardGroupCollectionArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            _uiManager.Show<CardGroupCompletedWindow>(args);
        }

        public void CloseSessionWindows()
        {
            if (_uiManager.IsWindowSpawned<CardCollectionController>())
            {
                var collectionWindow = _uiManager.GetWindowSync<CardCollectionController>();
                if (collectionWindow.IsShown)
                {
                    _uiManager.Hide<CardCollectionController>();
                }
            }

            if (_uiManager.IsWindowSpawned<NewCardController>())
            {
                var newCardWindow = _uiManager.GetWindowSync<NewCardController>();
                if (newCardWindow.IsShown)
                {
                    _uiManager.Hide<NewCardController>();
                }
            }

            if (_uiManager.IsWindowSpawned<CollectionStartedController>())
            {
                var startedWindow = _uiManager.GetWindowSync<CollectionStartedController>();
                if (startedWindow.IsShown)
                {
                    _uiManager.Hide<CollectionStartedController>(true);
                }
            }
        }
    }
}
