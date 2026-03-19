using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Core;
using UnityEngine;
using VContainer;

namespace core
{
    public sealed class OrchestratorRunner : MonoBehaviour
    {
        [SerializeField] private int _tickIntervalSeconds = 1;
        [SerializeField] private EventHudButtonPresenter _buttonPresenter;

        private CancellationToken _destroyToken;
        private EventOrchestrator _orchestrator;

        [Inject]
        private void Construct(EventOrchestrator orchestrator)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        private void Awake()
        {
            _destroyToken = this.GetCancellationTokenOnDestroy();
        }

        private void Start()
        {
            BindHudPresenters(_orchestrator);
            RunAsync(_destroyToken).Forget();
        }

        private async UniTask RunAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            await _orchestrator.InitializeAsync(ct);

            while (!ct.IsCancellationRequested)
            {
                await _orchestrator.TickAsync(ct);
                await UniTask.Delay(TimeSpan.FromSeconds(_tickIntervalSeconds), cancellationToken: ct);
            }
        }

        private void BindHudPresenters(EventOrchestrator orchestrator)
        {
            _buttonPresenter.Bind(orchestrator);
        }
    }
}
