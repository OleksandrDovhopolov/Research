using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIShared;
using UISystem;
using UnityEngine;
using VContainer;

namespace Game.Bootstrap.MainScene
{
    //TODO delete this class 
    public sealed class MainSceneBootstrap : MonoBehaviour
    {
        private UIManager _uiManager;
        private IGameplayReadyGate _gameplayReadyGate;
        
        private CancellationToken _destroyToken;
        
        [Inject]
        public void Install(UIManager uiManager, IGameplayReadyGate  gameplayReadyGate)
        {
            _uiManager = uiManager;
            _gameplayReadyGate = gameplayReadyGate;
        }

        private void Awake()
        {
            _destroyToken = this.GetCancellationTokenOnDestroy();
        }

        private void Start()
        {
            LoadGameplayAsync(_destroyToken).Forget();
        }
        
        private async UniTaskVoid LoadGameplayAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                _uiManager.Show<GameplaySceneController>();
                await UniTask.WaitUntil(() => _uiManager.IsWindowShown<GameplaySceneController>(), cancellationToken: ct);
                ct.ThrowIfCancellationRequested();

                await _gameplayReadyGate.MarkReadyAsync(ct);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
