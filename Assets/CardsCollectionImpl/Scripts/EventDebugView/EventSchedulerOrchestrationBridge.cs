using System;
using CardCollectionImpl;
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

        [Inject]
        private void Construct(IClock clock, UIManager uiManager, EventOrchestrator orchestrator, IGlobalTimerService  globalTimerService)
        {
            _clock = clock;
            _uiManager = uiManager;
            _orchestrator = orchestrator;
            _globalTimerService = globalTimerService;
        }

        private void Start()
        {
            if (_orchestrator == null || _globalTimerService == null)
            {
                Debug.LogWarning("EventSchedulerOrchestrationBridge has not been constructed yet.");
                return;
            }

            _orchestrator.OnEventCreated += HandleEventCreated;
            _orchestrator.OnEventStarted += HandleEventStarted;
            _orchestrator.OnEventCompleted += HandleEventCompleted;
        }
        
        private void HandleEventCreated(ScheduleItem item)
        {
            if (item == null) return;
            
            //Debug.LogWarning($"[Debug] ScheduleItem {item.Id}, {item.StartTimeUtc}");
            
            _globalTimerService.Register(item.Id, item.StartTimeUtc);
            
            if (_clock.UtcNow < item.StartTimeUtc)
            {
                if (_uiManager.IsWindowSpawned<GameplaySceneController>())
                {
                    var gameplaySceneController = _uiManager.GetWindowSync<GameplaySceneController>();
                    if (gameplaySceneController.IsShown)
                    {
                        gameplaySceneController.AddUpcomingEvent(item.Id, GetSpriteAddress(item.Id), _globalTimerService);
                    }
                }
            }
        }

        private string GetSpriteAddress(string eventId)
        {
            return eventId + "/" + CardCollectionGeneralConfig.CollectionPreview;
        }
        
        private void HandleEventStarted(ScheduleItem item)
        {
            if (item == null) return;
            
            _globalTimerService.Register(item.Id, item.EndTimeUtc);
            
            if (_uiManager.IsWindowSpawned<GameplaySceneController>())
            {
                var gameplaySceneController = _uiManager.GetWindowSync<GameplaySceneController>();
                if (gameplaySceneController.IsShown)
                {
                    gameplaySceneController.RemoveEventById(item.Id);
                }
            }
        }
        
        private void HandleEventCompleted(ScheduleItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id))
                return;

            _globalTimerService.Unregister(item.Id);
        }

        public void Dispose()
        {
            _orchestrator.OnEventCreated -= HandleEventCreated;
            _orchestrator.OnEventStarted -= HandleEventStarted;
            _orchestrator.OnEventCompleted -= HandleEventCompleted;
        }
    }
}
