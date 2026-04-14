using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using UIShared;
using UnityEngine;
using VContainer;

namespace EventOrchestration
{
    public sealed class OrchestratorRunner : MonoBehaviour
    {
        [SerializeField] private float _tickIntervalSeconds = 1f;

        private CancellationToken _destroyToken;
        private EventOrchestrator _orchestrator;
        private EventOrchestratorDebugFacade _debugFacade;
        private IGameplayReadyGate _gameplayReadyGate;

        private TimeSpan _timeSpan;

        [Inject]
        private void Construct(EventOrchestrator orchestrator, EventOrchestratorDebugFacade debugFacade, IGameplayReadyGate gameplayReadyGate)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _debugFacade = debugFacade ?? throw new ArgumentNullException(nameof(debugFacade));
            _gameplayReadyGate = gameplayReadyGate ?? throw new ArgumentNullException(nameof(gameplayReadyGate));
        }

        private void Awake()
        {
            _destroyToken = this.GetCancellationTokenOnDestroy();
            _timeSpan = TimeSpan.FromSeconds(_tickIntervalSeconds);
        }

        private void Start()
        {
            RunAsync(_destroyToken).Forget();
        }

        private async UniTask RunAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            await _gameplayReadyGate.WaitUntilReadyAsync(ct);

            await _orchestrator.InitializeAsync(ct);

            while (!ct.IsCancellationRequested)
            {
                await _orchestrator.TickAsync(ct);
                await UniTask.Delay(_timeSpan, cancellationToken: ct);
            }
        }

        public void AddDebugCardCollectionEventNextMinute(ScheduleItem scheduleItem)
        {
            AddDebugCardCollectionEventNextMinuteAsync(scheduleItem).Forget();
        }

        public UniTask AddDebugCardCollectionEventNextMinuteAsync(ScheduleItem scheduleItem)
        {
            _destroyToken.ThrowIfCancellationRequested();

            _debugFacade.AddScheduleItemForDebugAsync(scheduleItem, _destroyToken).Forget();
            return UniTask.CompletedTask;
        }

        public void CompleteCurrentEvent()
        {
            DebugCompleteCurrentEventAsync().Forget();
        }

        public void ForceNextEvent()
        {
            DebugForceEventAsync().Forget();
        }

        public UniTask DebugCompleteCurrentEventAsync()
        {
            _destroyToken.ThrowIfCancellationRequested();

            _debugFacade.DebugCompleteCurrentEventAsync(_destroyToken).Forget();
            return UniTask.CompletedTask;
        }

        public UniTask DebugForceEventAsync()
        {
            _destroyToken.ThrowIfCancellationRequested();

            _debugFacade.ForceNextEventAsync(_destroyToken).Forget();
            return UniTask.CompletedTask;
        }
    }
}
