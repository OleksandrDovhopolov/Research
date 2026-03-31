using System;
using EventOrchestration.Models;
using UIShared;
using UnityEngine;
using VContainer;

namespace EventOrchestration.Core
{
    public sealed class EventSchedulerOrchestrationBridge : MonoBehaviour, IDisposable
    {
        [SerializeField] private EventDebugView _debugView;
        
        private EventOrchestrator _orchestrator;
        private IGlobalTimerService _globalTimerService;

        [Inject]
        private void Construct(EventOrchestrator orchestrator, IGlobalTimerService  globalTimerService)
        {
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
            
            _globalTimerService.Register(item.Id, item.StartTimeUtc);
            _debugView.AddUpcoming(item.Id, _globalTimerService);
        }

        private void HandleEventStarted(ScheduleItem item)
        {
            if (item == null) return;
            
            _globalTimerService.Register(item.Id, item.EndTimeUtc);
            _debugView.OnEventStarted(item.Id);
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
