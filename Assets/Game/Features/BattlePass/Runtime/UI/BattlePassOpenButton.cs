using EventOrchestration;
using GameplayUI;
using UISystem;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BattlePass
{
    public sealed class BattlePassOpenButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private EventTimerDisplay _eventTimerDisplay;

        private UIManager _uiManager;
        private IBattlePassLifecycleState _lifecycleState;
        private EventOrchestrator _eventOrchestrator;
        private IGlobalTimerService _globalTimerService;
        private bool _isStarted;

        [Inject]
        private void Construct(
            UIManager uiManager,
            IBattlePassLifecycleState lifecycleState,
            EventOrchestrator eventOrchestrator,
            IGlobalTimerService globalTimerService)
        {
            _uiManager = uiManager;
            _lifecycleState = lifecycleState;
            _eventOrchestrator = eventOrchestrator;
            _globalTimerService = globalTimerService;
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
            UnbindTimer();
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
            RefreshTimer(displayStatus);
        }

        private void RefreshTimer(BattlePassLifecycleStatus displayStatus)
        {
            if (displayStatus != BattlePassLifecycleStatus.Active ||
                _eventOrchestrator == null ||
                _globalTimerService == null ||
                !_eventOrchestrator.TryGetCurrentEvent(BattlePassLiveOpsController.EventTypeValue, out var activeBattlePassItem))
            {
                UnbindTimer();
                return;
            }

            _eventTimerDisplay.Bind(activeBattlePassItem.Id, _globalTimerService);
        }

        private void UnbindTimer()
        {
            _eventTimerDisplay.Unbind();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            UnbindTimer();

            _button.onClick.RemoveListener(HandleClicked);
        }
    }
}
