using Unity.Entities;
using UIShared;
using UISystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace Survival
{
    // Watches the ECS world for PlayerDeadTag, opens the Game Over modal,
    // and reloads the scene on Restart.
    public sealed class GameOverBridge : MonoBehaviour
    {
        private UIManager _uiManager;
        private EntityQuery _deadQuery;
        private bool _modalOpen;

        [Inject]
        private void Construct(UIManager uiManager)
        {
            _uiManager = uiManager;
        }

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            _deadQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PlayerDeadTag>());
        }

        private void Update()
        {
            if (_modalOpen) return;
            if (_uiManager == null) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            if (_deadQuery.IsEmpty) return;

            _modalOpen = true;
            Time.timeScale = 0f;
            _uiManager.Show<GameOverController>(new GameOverArgs(OnRestart));
        }

        private void OnRestart()
        {
            Time.timeScale = 1f;
            _modalOpen = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
