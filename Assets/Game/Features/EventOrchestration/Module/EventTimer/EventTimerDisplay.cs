using System;
using TMPro;
using UnityEngine;
using VContainer;

namespace UIShared
{
    public sealed class EventTimerDisplay : MonoBehaviour
    {
        [SerializeField] private string _targetEventId;
        [SerializeField] private TextMeshProUGUI _timerText;

        private string _eventId;
        private IGlobalTimerService _globalTimerService;

        [Inject]
        private void Construct(IGlobalTimerService globalTimerService)
        {
            _globalTimerService = globalTimerService;
        }

        private void Awake()
        {
            _eventId = _targetEventId;
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshFromService();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Bind(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event id cannot be null or empty.", nameof(eventId));

            if (isActiveAndEnabled)
                Unsubscribe();

            _eventId = eventId;

            if (isActiveAndEnabled)
            {
                Subscribe();
                RefreshFromService();
            }
        }

        private void Subscribe()
        {
            if (_globalTimerService == null || string.IsNullOrEmpty(_eventId))
                return;

            _globalTimerService.OnTick += HandleTick;
            _globalTimerService.OnTimerFinished += HandleFinished;
        }

        private void Unsubscribe()
        {
            if (_globalTimerService == null)
                return;

            _globalTimerService.OnTick -= HandleTick;
            _globalTimerService.OnTimerFinished -= HandleFinished;
        }

        private void HandleTick(string eventId, TimeSpan remaining)
        {
            if (eventId != _eventId || _timerText == null)
                return;

            _timerText.text = TimeFormatter.Format(remaining);
        }

        private void HandleFinished(string eventId)
        {
            if (eventId != _eventId || _timerText == null)
                return;

            _timerText.text = TimeFormatter.Format(TimeSpan.Zero);
        }

        private void RefreshFromService()
        {
            if (string.IsNullOrEmpty(_eventId) || _timerText == null || _globalTimerService == null)
                return;

            if (_globalTimerService.TryGetRemaining(_eventId, out var remaining))
                _timerText.text = TimeFormatter.Format(remaining);
        }
    }
}
