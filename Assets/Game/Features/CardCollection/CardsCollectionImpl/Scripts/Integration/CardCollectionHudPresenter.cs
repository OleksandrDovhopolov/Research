using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using HUD;
using UIShared;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionHudPresenter : IDisposable
    {
        private const string CardCollectionButtonId = "CardCollection" + "/" + CardCollectionGeneralConfig.CollectionEventButton;
        
        private readonly UIManager _uiManager;
        private readonly IHUDService _hudService;
        
        private Func<CancellationToken, UniTask> _showCollectionHandler;
        
        private IEventButton _eventButton;
        
        public CardCollectionHudPresenter(UIManager uiManager, IHUDService hudService)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _hudService = hudService ?? throw new ArgumentNullException(nameof(hudService));
        }

        public void SetShowCollectionHandler(Func<CancellationToken, UniTask> showCollectionHandler)
        {
            _showCollectionHandler = showCollectionHandler ?? throw new ArgumentNullException(nameof(showCollectionHandler));
        }

        public void Bind(ScheduleItem config, CancellationToken ct)
        {
            BindASync(config, ct).Forget();
        }
        
        public async UniTask BindASync(ScheduleItem config, CancellationToken ct)
        {
            //TODO on event transition button can be shown when GameplaySceneController is still shown and timer not works in EventButton
            await UniTask.WaitForSeconds(1f, cancellationToken: ct);
            await UniTask.WaitUntil(() => _uiManager.IsWindowShown<GameplaySceneController>(), cancellationToken: ct);
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
            if (_eventButton == null) return;
            
            _eventButton = null;
            _hudService.RemoveEventButton(CardCollectionButtonId);
        }

        public void Dispose()
        {
            Unbind();
        }
    }
}