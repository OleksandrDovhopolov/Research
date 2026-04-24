using UISystem;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BattlePass
{
    public sealed class BattlePassOpenButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private UIManager _uiManager;

        [Inject]
        private void Construct(UIManager uiManager)
        {
            _uiManager = uiManager;
        }

        private void Start()
        {
            if (_button == null)
            {
                Debug.LogError("[BattlePassOpenButton] Button is not assigned.");
                return;
            }

            _button.onClick.AddListener(HandleClicked);
        }

        private void HandleClicked()
        {
            if (_uiManager == null)
            {
                Debug.LogWarning("[BattlePassOpenButton] UIManager is not injected.");
                return;
            }

            _uiManager.Show<BattlePassWindowController>();
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
            }
        }
    }
}
