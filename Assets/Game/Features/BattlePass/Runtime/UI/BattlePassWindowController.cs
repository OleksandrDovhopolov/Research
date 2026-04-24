using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;
using VContainer;

namespace BattlePass
{
    [Window("BattlePassWindow")]
    public class BattlePassWindowController : WindowController<BattlePassView>
    {
        private IBattlePassServerService _battlePassServerService;
        private IBattlePassTimerService _battlePassTimerService;
        private BattlePassUiModelFactory _uiModelFactory;

        private CancellationTokenSource _loadCts;

        [Inject]
        private void Construct(
            IBattlePassServerService battlePassServerService,
            IBattlePassTimerService battlePassTimerService,
            BattlePassUiModelFactory uiModelFactory)
        {
            _battlePassServerService = battlePassServerService;
            _battlePassTimerService = battlePassTimerService;
            _uiModelFactory = uiModelFactory;
        }

        protected override void OnShowStart()
        {
            ResetLoadCts();
            SubscribeTimer();
            View.ResetView();
            LoadBattlePassAsync(_loadCts.Token).Forget();
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.BuyPremiumClick += HandleBuyPremiumClicked;
            View.BuyPlatinumClick += HandleBuyPlatinumClicked;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.BuyPlatinumClick -= HandleBuyPlatinumClicked;
            View.BuyPremiumClick -= HandleBuyPremiumClicked;
            View.CloseClick -= CloseWindow;

            CancelLoad();
            UnsubscribeTimer();
            _battlePassTimerService?.Stop();
            View.ResetView();
        }

        private async UniTaskVoid LoadBattlePassAsync(CancellationToken ct)
        {
            try
            {
                var snapshot = await _battlePassServerService.GetCurrentAsync(ct);
                ct.ThrowIfCancellationRequested();

                if (snapshot?.Season == null)
                {
                    View.ShowUnavailableState(BattlePassConfig.Ui.UnavailableText);
                    return;
                }

                var uiModel = _uiModelFactory.Create(snapshot);
                View.Render(uiModel);
                _battlePassTimerService.Start(snapshot.ServerTimeUtc, snapshot.Season.EndAtUtc);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BattlePassWindowController] Failed to load Battle Pass data. {exception}");
                View.ShowUnavailableState(BattlePassConfig.Ui.UnavailableText);
            }
        }

        private void HandleTimerUpdated(TimeSpan remaining)
        {
            View.SetTimer(remaining);
        }

        private void HandleBuyPremiumClicked()
        {
        }

        private void HandleBuyPlatinumClicked()
        {
        }

        private void CloseWindow()
        {
            UIManager.Hide<BattlePassWindowController>();
        }

        private void SubscribeTimer()
        {
            if (_battlePassTimerService == null)
            {
                return;
            }

            _battlePassTimerService.OnTimerUpdated -= HandleTimerUpdated;
            _battlePassTimerService.OnTimerUpdated += HandleTimerUpdated;
        }

        private void UnsubscribeTimer()
        {
            if (_battlePassTimerService == null)
            {
                return;
            }

            _battlePassTimerService.OnTimerUpdated -= HandleTimerUpdated;
        }

        private void ResetLoadCts()
        {
            CancelLoad();
            _loadCts = new CancellationTokenSource();
        }

        private void CancelLoad()
        {
            if (_loadCts == null)
            {
                return;
            }

            _loadCts.Cancel();
            _loadCts.Dispose();
            _loadCts = null;
        }
    }
}
