using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UISystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GameplayUI
{
    public class HUDService : MonoBehaviour, IHUDService
    {
        [SerializeField] private GameObject _buttonPrefab;

        private readonly Dictionary<string, EventButton> _activeButtons = new();
        
        private UIManager _uiManager;
        private IObjectResolver _resolver;
        
        [Inject]
        private void Construct(IObjectResolver resolver, UIManager uiManager)
        {
            _resolver = resolver;
            _uiManager = uiManager;
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

            var gameplaySceneController = _uiManager.GetWindowSync<GameplaySceneController>();
            if (gameplaySceneController == null)
            {
                Debug.LogError("Failed to find GameplaySceneController");
                return null;
            }
            
            var btnObj = Instantiate(_buttonPrefab, gameplaySceneController.GetButtonContainer());
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
