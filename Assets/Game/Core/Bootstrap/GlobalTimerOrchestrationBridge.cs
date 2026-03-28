using EventOrchestration.Core;
using EventOrchestration.Models;
using UIShared;
using UnityEngine;
using VContainer;

namespace Game.Bootstrap
{
    public sealed class GlobalTimerOrchestrationBridge : MonoBehaviour
    {
        private EventOrchestrator _orchestrator;
        private IGlobalTimerService _globalTimerService;

        [Inject]
        private void Construct(EventOrchestrator orchestrator, IGlobalTimerService globalTimerService)
        {
            _orchestrator = orchestrator;
            _globalTimerService = globalTimerService;
        }

        private void Start()
        {
            if (_orchestrator == null)
                return;

            _orchestrator.OnEventStarted += OnEventStarted;
            _orchestrator.OnEventCompleted += OnEventCompleted;
        }

        private void OnDestroy()
        {
            if (_orchestrator == null)
                return;

            _orchestrator.OnEventStarted -= OnEventStarted;
            _orchestrator.OnEventCompleted -= OnEventCompleted;
        }

        private void OnEventStarted(ScheduleItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id))
                return;

            _globalTimerService.Register(item.Id, item.EndTimeUtc);
        }

        private void OnEventCompleted(ScheduleItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id))
                return;

            _globalTimerService.Unregister(item.Id);
        }
    }
}