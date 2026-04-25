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
        private IBattlePassLifecycleState _lifecycleState;
        private bool _isStarted;

        [Inject]
        private void Construct(UIManager uiManager, IBattlePassLifecycleState lifecycleState)
        {
            _uiManager = uiManager;
            _lifecycleState = lifecycleState;
        }

        private void Awake()
        {
            _button.onClick.AddListener(HandleClicked);
        }

        private void Start()
        {
            if (_button == null)
            {
                Debug.LogError("[BattlePassOpenButton] Button is not assigned.");
                return;
            }

            _isStarted = true;
            Subscribe();
            RefreshView();
        }

        private void OnEnable()
        {
            if (!_isStarted)
            {
                return;
            }

            Subscribe();
            RefreshView();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void HandleClicked()
        {
            if (_uiManager == null)
            {
                Debug.LogWarning("[BattlePassOpenButton] UIManager is not injected.");
                return;
            }

            Debug.LogWarning("[BattlePassOpenButton]Show BattlePassWindowController");
            //_uiManager.Show<BattlePassWindowController>();
        }

        private void Subscribe()
        {
            _lifecycleState.Changed -= RefreshView;
            _lifecycleState.Changed += RefreshView;
        }

        private void Unsubscribe()
        {
            _lifecycleState.Changed -= RefreshView;
        }

        private void RefreshView()
        {
            var displayStatus = _lifecycleState.CurrentStatus;

            _button.interactable = displayStatus != BattlePassLifecycleStatus.Inactive;
        }

        private void OnDestroy()
        {
            Unsubscribe();

            _button.onClick.RemoveListener(HandleClicked);
        }
    }
}
