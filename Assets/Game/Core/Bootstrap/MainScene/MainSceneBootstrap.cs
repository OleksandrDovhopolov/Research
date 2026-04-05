using UIShared;
using UISystem;
using UnityEngine;
using VContainer;

namespace Game.Bootstrap.MainScene
{
    //TODO delete this class 
    public sealed class MainSceneBootstrap : MonoBehaviour
    {
        private UIManager _uiManager;
        
        [Inject]
        public void Install(UIManager uiManager)
        {
            _uiManager = uiManager;
        }

        private void Start()
        {
            _uiManager.Show<GameplaySceneController>();
        }
    }
}
