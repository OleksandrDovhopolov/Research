using EventOrchestration;
using GameplayUI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UIShared;
using UISystem;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BattlePass
{
    public class BattlePassOpenButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Button _premiumButton;
        [SerializeField] private EventTimerDisplay _eventTimerDisplay;
        [SerializeField] private TMP_Text _seasonTitleText;
        [SerializeField] private Slider _xpSlider;

        private UIManager _uiManager;
        private IBattlePassLifecycleState _lifecycleState;
        private IBattlePassSnapshotStore _snapshotStore;
        private EventOrchestrator _eventOrchestrator;
        private IGlobalTimerService _globalTimerService;
        private bool _isStarted;

        [Inject]
        private void Construct(
            UIManager uiManager,
            IBattlePassLifecycleState lifecycleState,
            IBattlePassSnapshotStore snapshotStore,
            EventOrchestrator eventOrchestrator,
            IGlobalTimerService globalTimerService)
        {
            _uiManager = uiManager;
            _lifecycleState = lifecycleState;
            _snapshotStore = snapshotStore;
            _eventOrchestrator = eventOrchestrator;
            _globalTimerService = globalTimerService;
        }

        private void Awake()
        {
            _button.onClick.AddListener(HandleClicked);
            _premiumButton.onClick.AddListener(HandlePremiumClicked);
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
            ShowBattlePassWindow();
        }

        private void HandlePremiumClicked()
        {
            var decision = BattlePassPremiumEntryFlow.Resolve(
                _snapshotStore?.CurrentSnapshot,
                BattlePassPremiumOwnedBehavior.OpenBattlePassWindow);
            switch (decision.Action)
            {
                case BattlePassPremiumEntryAction.ShowInfo:
                    ShowInfo(decision.InfoMessage);
                    break;
                case BattlePassPremiumEntryAction.OpenBattlePassWindow:
                    ShowBattlePassWindow();
                    break;
                case BattlePassPremiumEntryAction.OpenPurchaseWindow:
                    ShowPremiumPurchaseWindow(new BattlePassIAPWindowArgs(
                        decision.SeasonId,
                        decision.ProductId,
                        HandlePremiumPurchaseVerified));
                    break;
            }
        }

        private void HandlePremiumPurchaseVerified(BattlePassPurchaseVerificationResult result)
        {
            if (result?.UpdatedUserState == null || _snapshotStore == null)
            {
                return;
            }

            _snapshotStore.TryApplyUserState(result.UpdatedUserState);
        }

        private void Subscribe()
        {
            if (_lifecycleState != null)
            {
                _lifecycleState.Changed -= RefreshView;
                _lifecycleState.Changed += RefreshView;
            }

            if (_snapshotStore != null)
            {
                _snapshotStore.SnapshotChanged -= HandleSnapshotChanged;
                _snapshotStore.SnapshotChanged += HandleSnapshotChanged;
            }
        }

        private void Unsubscribe()
        {
            if (_lifecycleState != null)
            {
                _lifecycleState.Changed -= RefreshView;
            }

            if (_snapshotStore != null)
            {
                _snapshotStore.SnapshotChanged -= HandleSnapshotChanged;
            }
        }

        private void RefreshView()
        {
            var displayStatus = _lifecycleState?.CurrentStatus ?? BattlePassLifecycleStatus.Inactive;

            _button.interactable = displayStatus != BattlePassLifecycleStatus.Inactive;
            if (_premiumButton != null)
            {
                _premiumButton.interactable = displayStatus != BattlePassLifecycleStatus.Inactive;
            }

            RefreshTimer(displayStatus);
            RefreshSeasonAndProgress();
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
            _eventTimerDisplay?.Unbind();
        }

        private void HandleSnapshotChanged(BattlePassSnapshot snapshot)
        {
            RefreshView();
        }

        private void RefreshSeasonAndProgress()
        {
            var snapshot = _snapshotStore?.CurrentSnapshot;
            _seasonTitleText.text = snapshot?.Season?.Title ?? string.Empty;

            SetXpProgress(ResolveNormalizedXpProgress(snapshot));
        }

        private void SetXpProgress(float normalizedProgress)
        {
            if (_xpSlider == null)
            {
                return;
            }

            _xpSlider.minValue = 0f;
            _xpSlider.maxValue = 1f;
            _xpSlider.wholeNumbers = false;
            _xpSlider.value = Mathf.Clamp01(normalizedProgress);
        }

        private static float ResolveNormalizedXpProgress(BattlePassSnapshot snapshot)
        {
            var userState = snapshot?.UserState;
            if (userState == null)
            {
                return 0f;
            }

            var orderedLevels = snapshot.Levels?
                .Where(level => level != null)
                .OrderBy(level => level.Level)
                .ToArray() ?? Array.Empty<BattlePassLevel>();
            var requiredXp = ResolveRequiredXp(orderedLevels, userState);
            var levelXpThresholds = orderedLevels
                .Select(level => Mathf.Max(0, level.XpRequired))
                .ToArray();

            return ResolveNormalizedXpProgress(
                userState.Xp,
                requiredXp,
                userState.Level,
                levelXpThresholds);
        }

        private static float ResolveNormalizedXpProgress(
            int currentXp,
            int requiredXp,
            int currentLevel,
            IReadOnlyList<int> levelXpThresholds)
        {
            var safeCurrentXp = Mathf.Max(0, currentXp);
            var safeRequiredXp = Mathf.Max(safeCurrentXp, requiredXp);
            if (safeRequiredXp <= 0)
            {
                return 0f;
            }

            if (!TryGetLevelStartXp(currentLevel, levelXpThresholds, out var currentLevelStartXp))
            {
                return 0f;
            }

            var currentLevelEndXp = ResolveLevelEndXp(
                currentLevel,
                currentLevelStartXp,
                safeRequiredXp,
                levelXpThresholds,
                safeCurrentXp);
            var segmentSize = currentLevelEndXp - currentLevelStartXp;
            if (segmentSize <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)(safeCurrentXp - currentLevelStartXp) / segmentSize);
        }

        private static int ResolveLevelEndXp(
            int level,
            int currentLevelStartXp,
            int fallbackRequiredXp,
            IReadOnlyList<int> levelXpThresholds,
            int currentXp)
        {
            if (TryGetLevelStartXp(level + 1, levelXpThresholds, out var nextLevelStartXp))
            {
                return Mathf.Max(currentLevelStartXp, nextLevelStartXp);
            }

            return Mathf.Max(currentLevelStartXp, Mathf.Max(fallbackRequiredXp, currentXp));
        }

        private static bool TryGetLevelStartXp(int level, IReadOnlyList<int> levelXpThresholds, out int levelStartXp)
        {
            if (level < 0)
            {
                levelStartXp = 0;
                return false;
            }

            if (levelXpThresholds == null || levelXpThresholds.Count == 0)
            {
                levelStartXp = 0;
                return level == 0;
            }

            if (level >= levelXpThresholds.Count)
            {
                levelStartXp = 0;
                return false;
            }

            levelStartXp = Mathf.Max(0, levelXpThresholds[level]);
            return true;
        }

        private static int ResolveRequiredXp(IReadOnlyList<BattlePassLevel> orderedLevels, BattlePassUserState userState)
        {
            var currentXp = Mathf.Max(0, userState?.Xp ?? 0);
            var currentLevel = Mathf.Max(0, userState?.Level ?? 0);
            if (orderedLevels == null || orderedLevels.Count == 0)
            {
                return currentXp;
            }

            for (var i = 0; i < orderedLevels.Count; i++)
            {
                var level = orderedLevels[i];
                if (level == null || level.Level <= currentLevel)
                {
                    continue;
                }

                return Mathf.Max(currentXp, level.XpRequired);
            }

            return currentXp;
        }

        protected virtual void ShowInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _uiManager?.Show<InfoWidgetController>(new InfoWidgetArg(message));
        }

        protected virtual void ShowBattlePassWindow()
        {
            if (_uiManager == null)
            {
                Debug.LogWarning("[BattlePassOpenButton] UIManager is not injected.");
                return;
            }

            _uiManager.Show<BattlePassWindowController>();
        }

        protected virtual void ShowPremiumPurchaseWindow(BattlePassIAPWindowArgs args)
        {
            if (_uiManager == null)
            {
                Debug.LogWarning("[BattlePassOpenButton] UIManager is not injected.");
                return;
            }

            _uiManager.Show<BattlePassIAPWindowController>(args);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            UnbindTimer();

            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
            }

            if (_premiumButton != null)
            {
                _premiumButton.onClick.RemoveListener(HandlePremiumClicked);
            }
        }
    }
}
