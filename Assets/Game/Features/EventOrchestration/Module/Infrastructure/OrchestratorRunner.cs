using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using Newtonsoft.Json;
using UIShared;
using UnityEngine;
using VContainer;

namespace EventOrchestration
{
    public sealed class OrchestratorRunner : MonoBehaviour
    {
        private const string DebugLogPath = @"c:\Projects\Research\.cursor\debug.log";
        [SerializeField] private float _tickIntervalSeconds = 1f;

        private CancellationToken _destroyToken;
        private EventOrchestrator _orchestrator;
        private IGameplayReadyGate _gameplayReadyGate;

        private TimeSpan _timeSpan;
        
        [Inject]
        private void Construct(EventOrchestrator orchestrator, IGameplayReadyGate  gameplayReadyGate)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _gameplayReadyGate = gameplayReadyGate ?? throw new ArgumentNullException(nameof(gameplayReadyGate));
        }

        private void Awake()
        {
            _destroyToken = this.GetCancellationTokenOnDestroy();
            _timeSpan =  TimeSpan.FromSeconds(_tickIntervalSeconds);
        }

        private void Start()
        {
            RunAsync(_destroyToken).Forget();
        }

        private async UniTask RunAsync(CancellationToken ct)
        {
            #region agent log
            Debug.LogWarning("[Debug] OrchestratorRunner.RunAsync entered");
            WriteDebugLog("H1", "OrchestratorRunner.RunAsync", "[Debug] OrchestratorRunner.RunAsync entered", new
            {
                isCancellationRequested = ct.IsCancellationRequested,
                tickIntervalSeconds = _tickIntervalSeconds
            });
            #endregion

            ct.ThrowIfCancellationRequested();

            await _gameplayReadyGate.WaitUntilReadyAsync(ct); 

            #region agent log
            Debug.LogWarning("[Debug] OrchestratorRunner gate is ready, calling EventOrchestrator.InitializeAsync");
            WriteDebugLog("H2", "OrchestratorRunner.RunAsync", "[Debug] OrchestratorRunner gate is ready, calling EventOrchestrator.InitializeAsync");
            #endregion

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

            _orchestrator.AddScheduleItemForDebugAsync(scheduleItem, _destroyToken).Forget();
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

            _orchestrator.DebugCompleteCurrentEventAsync(_destroyToken).Forget();
            return UniTask.CompletedTask;
        }
        
        public UniTask DebugForceEventAsync()
        {
            _destroyToken.ThrowIfCancellationRequested();

            _orchestrator.ForceNextEventAsync(_destroyToken).Forget();
            return UniTask.CompletedTask;
        }

        private static void WriteDebugLog(string hypothesisId, string location, string message, object data = null)
        {
            try
            {
                var payload = new
                {
                    runId = "initial",
                    hypothesisId,
                    location,
                    message,
                    data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                };
                File.AppendAllText(DebugLogPath, JsonConvert.SerializeObject(payload) + Environment.NewLine);
            }
            catch
            {
                // Instrumentation must never break runtime flow.
            }
        }
    }
}
