using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using GameplayUI;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionHudPresenter : IDisposable
    {
        private const string CardCollectionButtonId = "CardCollection" + "/" + CardCollectionGeneralConfig.CollectionEventButton;
        
        private readonly UIManager _uiManager;
        
        private Func<CancellationToken, UniTask> _showCollectionHandler;
        
        private IEventButton _eventButton;
        private GameplaySceneController _gameplaySceneController;
        
        public CardCollectionHudPresenter(UIManager uiManager)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
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
            _gameplaySceneController ??= _uiManager.GetWindowSync<GameplaySceneController>();
            
            await UniTask.WaitForSeconds(1f, cancellationToken: ct);
            await UniTask.WaitUntil(() => _gameplaySceneController.IsShown, cancellationToken: ct);

            var entryButton =  _gameplaySceneController.GetEventButton();
            if (entryButton == null)
            {
                Debug.LogWarning($"[CardCollectionRuntime] No button found for {CardCollectionButtonId}]");
                return;
            }
            
            _eventButton = entryButton;
            _eventButton.SetVisible(true);
            _eventButton.Setup(config, () => OnEventButtonClickHandler(ct), ct);
        }

        private void OnEventButtonClickHandler(CancellationToken ct)
        {
            _showCollectionHandler?.Invoke(ct).Forget();
        }
        
        public void Unbind()
        {
            if (_eventButton == null) return;
            
            _eventButton.SetVisible(false);
            _eventButton = null;
        }

        public void Dispose()
        {
            Unbind();
        }
    }
}