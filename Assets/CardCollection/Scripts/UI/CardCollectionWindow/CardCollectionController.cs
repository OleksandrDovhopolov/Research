using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class CardCollectionArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        
        public CardCollectionArgs(UIManager uiManager)
        {
            UiManager = uiManager;
        }
    }
    
    [Window("CardCollectionWindow")]
    public class CardCollectionController :  WindowController<CardCollectionView>
    {
        private CardCollectionArgs Args => (CardCollectionArgs) Arguments;
        
        
        private bool _groupsCreated;
        
        protected override void OnShowStart()
        {
            if (_groupsCreated) return;
            
            View.ShowLoader(true); 
            View.CreateViews(CardGroupsConfigStorage.Instance.Data);
            //CreateGroupViews().Forget();
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.OnButtonPressed += OpenGroupWindow;
            
            if (_groupsCreated) return;
            CreateGroupViews().Forget();
        }

        private async UniTask CreateGroupViews()
        {
            try
            {
                await View.CreateGroupViews(CardGroupsConfigStorage.Instance.Data);
                _groupsCreated = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load groups: {e}");
            }
            finally
            {
                View.ShowLoader(false);
            }
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            View.OnButtonPressed -= OpenGroupWindow;
        }

        private void CloseWindow()
        {
            Args.UiManager.Hide<CardCollectionController>();
        }
        
        private void OpenGroupWindow()
        {
            var args = new CardGroupArgs(Args.UiManager);
            Args.UiManager.Show<CardGroupController>(args);
        }
    }
}