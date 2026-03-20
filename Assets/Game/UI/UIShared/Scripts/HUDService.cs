using System.Collections.Generic;
using UnityEngine;

namespace UIShared
{
    public class HUDService : MonoBehaviour, IHUDService
    {
        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private Transform _eventsContainer;

        private readonly Dictionary<string, EventButton> _activeButtons = new();
        
        public IEventButton SpawnEventButton(string eventId)
        {
            if (_activeButtons.TryGetValue(eventId, out var button))
            {
                return button.GetComponent<IEventButton>();
            }

            var btnObj = Instantiate(_buttonPrefab, _eventsContainer);
            var eventButton = btnObj.GetComponent<EventButton>();
            _activeButtons.Add(eventId, eventButton);

            return eventButton;
        }

        public void RemoveEventButton(string eventId)
        {
            if (_activeButtons.TryGetValue(eventId, out var eventButton)) {
                Destroy(eventButton.gameObject);
                _activeButtons.Remove(eventId);
            }
        }
    }
}