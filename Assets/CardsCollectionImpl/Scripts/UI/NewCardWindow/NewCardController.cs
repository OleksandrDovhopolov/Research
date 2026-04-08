using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UISystem;
using VContainer;

namespace CardCollectionImpl
{
    public class NewCardArgs : WindowArgs
    {
        public readonly string EventId;
        public readonly int Points;
        public readonly List<NewCardDisplayData> Cards;

        public NewCardArgs(string eventId, int points, List<NewCardDisplayData> cards)
        {
            EventId = eventId;
            Points = points;
            Cards = cards ?? new List<NewCardDisplayData>();
        }
    }

    [Window("NewCardWindow")]
    public class NewCardController : WindowController<NewCardView>
    {
        private IEventSpriteManager _eventSpriteManager;
        
        private NewCardArgs Args => (NewCardArgs)Arguments;
        private CancellationTokenSource _cts;

        [Inject]
        private void Construct(IEventSpriteManager eventSpriteManager)
        {
            _eventSpriteManager = eventSpriteManager;
        }
        
        protected override void OnShowStart()
        {
            _cts = new CancellationTokenSource();
            View.SetSpriteManager(_eventSpriteManager);
            View.UpdatePointsAmount(Args.Points);
            View.CreateNewCards(Args.EventId, Args.Cards);
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
            CloseWindowAsync(_cts.Token).Forget();
        }

        private async UniTask CloseWindowAsync(CancellationToken ct)
        {
            View.CloseClick -= CloseWindow;
            await View.PlayCloseSequenceAsync(ct);
            UIManager.Hide<NewCardController>();
        }
    }
}
