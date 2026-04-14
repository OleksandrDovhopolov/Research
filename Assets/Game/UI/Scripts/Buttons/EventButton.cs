using System;
using System.Threading;
using EventOrchestration;
using EventOrchestration.Models;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace GameplayUI
{
    public class EventButton : MonoBehaviour, IEventButton
    {
        [SerializeField] private Button _button;
        [SerializeField] private EventTimerDisplay _eventTimerDisplay;

        private IGlobalTimerService _globalTimerService;

        [Inject]
        private void Construct(IGlobalTimerService globalTimerService)
        {
            _globalTimerService = globalTimerService;
        }

        public void Setup(ScheduleItem config, Action onClick, CancellationToken ct)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick?.Invoke());

            _eventTimerDisplay.Bind(config.Id, _globalTimerService);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
        }
    }
}
