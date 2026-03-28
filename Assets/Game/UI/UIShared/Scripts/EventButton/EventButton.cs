using System;
using System.Threading;
using EventOrchestration.Models;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace UIShared
{
    public class EventButton : MonoBehaviour, IEventButton
    {
        [SerializeField] private Button _button;
        [SerializeField] private EventTimerDisplay _eventTimerDisplay;

        private IGlobalTimerService _globalTimerService;
        private string _registeredEventId;

        [Inject]
        private void Construct(IGlobalTimerService globalTimerService)
        {
            _globalTimerService = globalTimerService;
        }

        public void Setup(ScheduleItem config, Action onClick, CancellationToken ct)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick?.Invoke());

            if (!string.IsNullOrEmpty(_registeredEventId))
                _globalTimerService.Unregister(_registeredEventId);

            _registeredEventId = config.Id;
            _globalTimerService.Register(config.Id, config.EndTimeUtc);
            _eventTimerDisplay.Bind(config.Id);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();

            if (!string.IsNullOrEmpty(_registeredEventId))
                _globalTimerService?.Unregister(_registeredEventId);
        }
    }
}
