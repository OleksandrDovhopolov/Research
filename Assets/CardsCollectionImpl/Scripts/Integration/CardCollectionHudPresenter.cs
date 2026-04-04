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
        private const string CardCollectionButtonId = "CardCollection" + "/" + CardCollectionGeneralConfig.CollectionEventButton;
        
        private readonly IHUDService _hudService;
        private Func<CancellationToken, UniTask> _showCollectionHandler;
        
        private IEventButton _eventButton;
        
        public CardCollectionHudPresenter(IHUDService hudService)
        {
            _hudService = hudService ?? throw new ArgumentNullException(nameof(hudService));
        }

        public void SetShowCollectionHandler(Func<CancellationToken, UniTask> showCollectionHandler)
        {
            _showCollectionHandler = showCollectionHandler ?? throw new ArgumentNullException(nameof(showCollectionHandler));
        }

        public async UniTask Bind(ScheduleItem config, CancellationToken ct)
        {
            var entryButton = await _hudService.SpawnEventButtonAsync(CardCollectionButtonId, ct);

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
            _showCollectionHandler?.Invoke(ct).Forget();
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