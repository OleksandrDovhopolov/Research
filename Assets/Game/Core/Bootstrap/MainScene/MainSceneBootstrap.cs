using CoreResources;
using Game.Cheat;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Bootstrap.MainScene
{
    public sealed class MainSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private ResearchCheatModule _researchCheatModule;
        [SerializeField] private Button _cheatButton;
        [SerializeField] private ResourcesView _resourcesView;

        private ResourceManager _resourceManager;
        
        [Inject]
        private void Construct(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
        }

        private void Start()
        {
            _resourcesView.InitView(_resourceManager);
            _resourcesView.UpdateFromResourceManager(true);
        }

        private void OnEnable()
        {
            _cheatButton.onClick.AddListener(OpenCheatsPanel);
        }

        private void OnDisable()
        {
            _cheatButton.onClick.RemoveListener(OpenCheatsPanel);
        }

        private void OpenCheatsPanel()
        {
            _researchCheatModule?.OpenCheatPanel();
        }
    }
}
