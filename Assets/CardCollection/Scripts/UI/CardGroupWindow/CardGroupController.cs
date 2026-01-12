using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class CardGroupArgs : WindowArgs
    {
        public readonly string GroupType;
        public readonly UIManager UiManager;
        public readonly ICardUpdater CardUpdater;
        public readonly List<CardProgressData> GroupData;
        
        public CardGroupArgs(UIManager uiManager, ICardUpdater iCardUpdater, string groupType, List<CardProgressData> groupData)
        {
            GroupType = groupType;
            UiManager = uiManager;
            CardUpdater = iCardUpdater;
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

            SetCardSprites(data).Forget();
            
            ResetNewFlag();
        }

        private void ResetNewFlag()
        {
            foreach (var cardData in Args.GroupData.Where(cardData => cardData.IsNew))
            {
                Debug.LogWarning($"Debug Reset cardData.CardId {cardData.CardId}");
                Args.CardUpdater.ResetNewFlagAsync(cardData.CardId);
            }
        }
        
        private async UniTask SetCardSprites(List<CardCollectionConfig> cardsData)
        {
            await View.SetSprites(cardsData);
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