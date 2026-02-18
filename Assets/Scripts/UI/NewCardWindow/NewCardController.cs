using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class NewCardArgs : WindowArgs
    {
        public readonly CardPack CardPack;
        public readonly UIManager UiManager;
        public readonly ICardCollectionModule CollectionModule;

        public NewCardArgs(CardPack cardPack, UIManager uiManager, ICardCollectionModule collectionModule)
        {
            CardPack = cardPack;
            UiManager = uiManager;
            CollectionModule = collectionModule;
        }
    }

    [Window("NewCardWindow")]
    public class NewCardController : WindowController<NewCardView>
    {
        private NewCardArgs Args => (NewCardArgs)Arguments;
        private CancellationTokenSource _cts;

        protected override void OnShowStart()
        {
            _cts = new CancellationTokenSource();
            GetNewCardsAsync(_cts.Token).Forget();
        }

        private async UniTask GetNewCardsAsync(CancellationToken ct)
        {
            var cardsIdList = await Args.CollectionModule.OpenPackAndUnlockAsync(Args.CardPack, ct);
            var cardsData = await Args.CollectionModule.GetCardsByIdsAsync(cardsIdList, ct);
            var displayData = cardsData.ToNewCardDisplayData();
            
            View.CreateNewCards(displayData);
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        protected override void OnHideComplete(bool isClosed)
        {
            base.OnHideComplete(isClosed);

            View.DisableAll();
        }

        private void CloseWindow()
        {
            Args.UiManager.Hide<NewCardController>();
        }
    }
}
