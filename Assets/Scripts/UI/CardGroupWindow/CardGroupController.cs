using System.Collections.Generic;
using System.Linq;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class CardGroupArgs : WindowArgs
    {
        public readonly string GroupType;
        public readonly UIManager UiManager;
        public readonly ICardCollectionModule CardCollectionModule;
        public readonly List<CardProgressData> GroupData;
        public readonly int GroupNumber;
        public readonly int GroupsAmount;
        
        public CardGroupArgs(
            UIManager uiManager, 
            ICardCollectionModule cardCollectionModule, 
            string groupType, 
            List<CardProgressData> groupData,
            int groupNumber,
            int groupsAmount)
        {
            GroupType = groupType;
            UiManager = uiManager;
            CardCollectionModule = cardCollectionModule;
            GroupData = groupData;
            GroupNumber = groupNumber;
            GroupsAmount = groupsAmount;
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

            var collectionNumberText = "Set " + Args.GroupNumber + "/" + Args.GroupsAmount;
            View.SetCollectionNumber(collectionNumberText);

            SetCardSprites(data).Forget();
            
            ResetNewFlag();
        }

        private void ResetNewFlag()
        {
            foreach (var cardData in Args.GroupData.Where(cardData => cardData.IsNew))
            {
                Args.CardCollectionModule.ResetNewFlagAsync(cardData.CardId);
            }
        }
        
        private async UniTask SetCardSprites(List<CardCollectionConfig> cardsData)
        {
            await View.SetSprites(cardsData);
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.OnLeftClick += OnLeftClickHandler;
            View.OnRightClick += OnRightClickHandler;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            View.OnLeftClick -= OnLeftClickHandler;
            View.OnRightClick -= OnRightClickHandler;
            View.DisableAll();
        }


        private void OnLeftClickHandler()
        {
            Debug.LogWarning($"Debug Left clicked");
        }

        private void OnRightClickHandler()
        {
            Debug.LogWarning($"Debug Right clicked");
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