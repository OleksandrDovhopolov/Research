using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using UIShared;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionHudPresenter : IDisposable
    {
        private const string CardCollectionButtonId = "CardCollection";
        
        private readonly IHUDService _hudService;
        private readonly ICardCollectionWindowOpener _cardCollectionWindowOpener;
        
        private IEventButton _eventButton;
        
        public CardCollectionHudPresenter(IHUDService hudService, ICardCollectionWindowOpener cardCollectionWindowOpener)
        {
            _hudService = hudService ?? throw new ArgumentNullException(nameof(hudService));
            _cardCollectionWindowOpener = cardCollectionWindowOpener ?? throw new ArgumentNullException(nameof(cardCollectionWindowOpener));
        }

        public void Bind(ScheduleItem config, CancellationToken ct)
        {
            var spriteAddress = CardCollectionButtonId + "/" + CardCollectionGeneralConfig.CollectionEventButton;
            var entryButton = _hudService.SpawnEventButton(CardCollectionButtonId, spriteAddress);

            if (entryButton == null)
            {
                Debug.LogWarning($"[CardCollectionRuntime] No button found for {CardCollectionButtonId}]");
                return;
            }
            
            _eventButton = entryButton;
            _eventButton.Setup(config, () => OnEventButtonClickHandler(ct), ct);
            _eventButton.SetVisible(true);
        }

        private void OnEventButtonClickHandler(CancellationToken ct)
        {
            _cardCollectionWindowOpener.OpenCardCollectionWindow(ct).Forget();
        }
        
        public void Unbind()
        {
            _eventButton = null;
            _hudService.RemoveEventButton(CardCollectionButtonId);
        }

        public void Dispose()
        {
            Unbind();
        }
    }
}