using core;
using CoreResources;
using Game.Cheat;
using UISystem;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Bootstrap.MainScene
{
    //TODO delete this class 
    public sealed class MainSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private ResearchCheatModule _researchCheatModule;
        [SerializeField] private Button _cheatButton; // TODO move into ResearchCheatModule
        [SerializeField] private ResourcesView _resourcesView;

        private UIManager _uiManager;
        private IObjectResolver _resolver;
        private ResourceManager _resourceManager;
        
        [Inject]
        private void Construct(ResourceManager resourceManager, UIManager uiManager, IObjectResolver  resolver)
        {
            _resolver = resolver;
            _uiManager = uiManager;
            _resourceManager = resourceManager;
        }

        private void Start()
        {
            var windowFactoryBase = new WindowFactoryDI(_uiManager, _resolver);
            var eventHandler = new UIManagerSignalHandler();
            _uiManager.Configurate(windowFactoryBase, eventHandler);
            
            //TODO move inject in _resourcesView
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
