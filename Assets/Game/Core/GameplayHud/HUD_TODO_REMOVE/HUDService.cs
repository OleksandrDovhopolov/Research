using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIShared
{
    //TODO place it in correct ASMDEF
    public class HUDService : MonoBehaviour, IHUDService
    {
        [SerializeField] private GameObject _buttonPrefab;

        private readonly Dictionary<string, EventButton> _activeButtons = new();
        private IObjectResolver _resolver;

        private Transform _eventsContainerTransform;
        
        [Inject]
        private void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public async UniTask<IEventButton> SpawnEventButtonAsync(string spriteAddress, CancellationToken ct)
        {
            if (_activeButtons.TryGetValue(spriteAddress, out var button))
            {
                return button.GetComponent<IEventButton>();
            }

            var wasPrefabActive = _buttonPrefab.activeSelf;
            _buttonPrefab.SetActive(false);

            var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(spriteAddress, ct);

            if (_eventsContainerTransform == null)
            {
                //TODO fix it
                _eventsContainerTransform = GameObject.Find("EventsContainer").transform;
                if (_eventsContainerTransform == null)
                {
                    Debug.LogError("Events Container not found");
                    return null;
                }
            }
            
            var btnObj = Instantiate(_buttonPrefab, _eventsContainerTransform);
            _buttonPrefab.SetActive(wasPrefabActive);

            _resolver.InjectGameObject(btnObj);
            btnObj.SetActive(wasPrefabActive);

            var eventButton = btnObj.GetComponent<EventButton>();
            eventButton.SetSprite(sprite);
            _activeButtons.Add(spriteAddress, eventButton);

            return eventButton;
        }
        
        public void RemoveEventButton(string eventId)
        {
            if (_activeButtons.TryGetValue(eventId, out var eventButton))
            {
                ProdAddressablesWrapper.Release(eventId);
                Destroy(eventButton.gameObject);
                _activeButtons.Remove(eventId);
            }
        }
    }
}
