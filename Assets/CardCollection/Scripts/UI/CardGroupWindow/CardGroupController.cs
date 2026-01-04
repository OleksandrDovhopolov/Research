using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class CardGroupArgs : WindowArgs
    {
        public readonly string GroupType;
        public readonly UIManager UiManager;
        
        public CardGroupArgs(UIManager uiManager, string groupType)
        {
            GroupType = groupType;
            UiManager = uiManager;
        }
    }
    
    [Window("CardGroupWindow", WindowType.Popup)]
    public class CardGroupController :  WindowController<CardGroupView>
    {
        private UIManager _uiManager;
        
        private CardGroupArgs Args => (CardGroupArgs) Arguments;
        
        protected override void OnShowStart()
        {
            var data = CardCollectionConfigStorage.Instance.Get(Args.GroupType);
            Debug.LogWarning($"Debug groupType {Args.GroupType}, cardConfigs {data.Count}");
            View.CreateViews(data);

            CreateGroupViews(data).Forget();
        }
        
        private async UniTask CreateGroupViews(List<CardCollectionConfig> data)
        {
            await View.SetSprites(data);
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
            View.DisableAll();
        }

        private void CloseWindow()
        {
            Args.UiManager.Hide<CardGroupController>();
        }
    }
}