using System;
using System.Threading;
using CardCollectionImpl;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
using UIShared;
using UISystem;
using UnityEngine;
using VContainer;

namespace EventOrchestration.Core
{
    public sealed class EventSchedulerOrchestrationBridge : MonoBehaviour, IDisposable
    {
        private IClock _clock;
        private UIManager _uiManager;
        private EventOrchestrator _orchestrator;
        private IGlobalTimerService _globalTimerService;
        
        private CancellationToken _destroyToken;

        [Inject]
        private void Construct(IClock clock, UIManager uiManager, EventOrchestrator orchestrator, IGlobalTimerService  globalTimerService)
        {
            _clock = clock;
            _uiManager = uiManager;
            _orchestrator = orchestrator;
            _globalTimerService = globalTimerService;
        }

        private void Awake()
        {
            _destroyToken = this.GetCancellationTokenOnDestroy();
        }

        private void Start()
        {
            if (_orchestrator == null || _globalTimerService == null)
            {
                Debug.LogWarning("EventSchedulerOrchestrationBridge has not been constructed yet.");
                return;
            }

            _orchestrator.OnEventCreated += HandleEventCreated;
            _orchestrator.OnEventStarting += HandleEventStarting;
            _orchestrator.OnEventStarted += HandleEventStarted;
            _orchestrator.OnEventCompleted += HandleEventCompleted;

            SyncWhenOrchestratorReadyAsync(_destroyToken).Forget();
        }
        
        private void HandleEventCreated(ScheduleItem item)
        {
            if (item == null) return;
            
            _globalTimerService.Register(item.Id, item.StartTimeUtc);
            
            if (_clock.UtcNow < item.StartTimeUtc)
            {
                if (_uiManager.IsWindowSpawned<GameplaySceneController>())
                {
                    var gameplaySceneController = _uiManager.GetWindowSync<GameplaySceneController>();
                    var spriteAddress = item.Id + "/" + CardCollectionGeneralConfig.CollectionPreview;
                    gameplaySceneController.AddUpcomingEvent(item.Id, spriteAddress, _globalTimerService);
                }
            }
        }

        private void HandleEventStarting(ScheduleItem item)
        {
            if (item == null) return;
            
            Debug.LogWarning($"[Debug] HandleEventStarting item {item.Id}, {item.EndTimeUtc}");
            _globalTimerService.Register(item.Id, item.EndTimeUtc);
        }
        
        private void HandleEventStarted(ScheduleItem item)
        {
            if (item == null) return;
            
            if (_uiManager.IsWindowSpawned<GameplaySceneController>())
            {
                var gameplaySceneController = _uiManager.GetWindowSync<GameplaySceneController>();
                gameplaySceneController.RemoveEventById(item.Id);
            }
        }
        
        private void HandleEventCompleted(ScheduleItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id))
                return;

            _globalTimerService.Unregister(item.Id);
        }

        private void SyncFromOrchestratorSnapshot()
        {
            var scheduleItems = _orchestrator.GetScheduleSnapshot();
            foreach (var item in scheduleItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Id))
                {
                    continue;
                }

                if (!_orchestrator.TryGetStateSnapshot(item.Id, out var state))
                {
                    continue;
                }

                switch (state.State)
                {
                    case EventInstanceState.Pending:
                        HandleEventCreated(item);
                        break;
                    case EventInstanceState.Starting:
                    case EventInstanceState.Active:
                    case EventInstanceState.Ending:
                    case EventInstanceState.Settling:
                        HandleEventStarted(item);
                        break;
                    case EventInstanceState.Completed:
                    case EventInstanceState.Failed:
                    case EventInstanceState.Cancelled:
                        HandleEventCompleted(item);
                        break;
                }
            }
        }

        private async UniTaskVoid SyncWhenOrchestratorReadyAsync(CancellationToken ct)
        {
            try
            {
                await _orchestrator.WaitUntilInitializedAsync(ct);
                SyncFromOrchestratorSnapshot();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventSchedulerOrchestrationBridge] Failed to sync from orchestrator snapshot: {ex}");
            }
        }

        public void Dispose()
        {
            _orchestrator.OnEventCreated -= HandleEventCreated;
            _orchestrator.OnEventStarting -= HandleEventStarting;
            _orchestrator.OnEventStarted -= HandleEventStarted;
            _orchestrator.OnEventCompleted -= HandleEventCompleted;
        }
    }
}
