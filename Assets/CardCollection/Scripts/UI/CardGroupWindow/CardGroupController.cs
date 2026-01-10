using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UISystem;

namespace core
{
    public class CardGroupArgs : WindowArgs
    {
        public readonly string GroupType;
        public readonly UIManager UiManager;
        public readonly List<CardProgressData> GroupData;
        
        public CardGroupArgs(UIManager uiManager, string groupType, List<CardProgressData> groupData)
        {
            GroupType = groupType;
            UiManager = uiManager;
            GroupData = groupData;
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
            View.CreateDataViews(Args.GroupType, Args.GroupData);

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
            View.DisableAll();
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