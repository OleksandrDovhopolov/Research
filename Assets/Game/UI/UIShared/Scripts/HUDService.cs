using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIShared
{
    public class HUDService : MonoBehaviour, IHUDService
    {
        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private Transform _eventsContainer;

        private readonly Dictionary<string, EventButton> _activeButtons = new();
        private IObjectResolver _resolver;

        [Inject]
        private void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public IEventButton SpawnEventButton(string eventId)
        {
            if (_activeButtons.TryGetValue(eventId, out var button))
            {
                return button.GetComponent<IEventButton>();
            }

            var wasPrefabActive = _buttonPrefab.activeSelf;
            _buttonPrefab.SetActive(false);

            var btnObj = Instantiate(_buttonPrefab, _eventsContainer);
            _buttonPrefab.SetActive(wasPrefabActive);

            _resolver.InjectGameObject(btnObj);
            btnObj.SetActive(wasPrefabActive);

            var eventButton = btnObj.GetComponent<EventButton>();
            _activeButtons.Add(eventId, eventButton);

            return eventButton;
        }

        public void RemoveEventButton(string eventId)
        {
            if (_activeButtons.TryGetValue(eventId, out var eventButton))
            {
                Destroy(eventButton.gameObject);
                _activeButtons.Remove(eventId);
            }
        }
    }
}
