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
        
        protected override void OnShowStart()
        {
            //var groupsConfigs = CardGroupsConfigStorage.Instance.Data;
            CreateGroupViews().Forget(); // Fire & forget ✅
            /*View.ShowLoader(true); 
    
            try 
            {
                await View.CreateGroupViews(groupsConfigs);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load groups: {e}");
            }
            finally 
            {
                View.ShowLoader(false); 
            }*/
        }
        
        private async UniTask CreateGroupViews()
        {
            var groupsConfigs = CardGroupsConfigStorage.Instance.Data;
            
            View.ShowLoader(true); 
    
            try 
            {
                await View.CreateGroupViews(groupsConfigs);
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
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.OnButtonPressed += OpenGroupWindow;
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