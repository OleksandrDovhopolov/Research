using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using Infrastructure;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardCollectionSession : IDisposable
    {
        private readonly CardCollectionModule _module;
        private readonly ICardCollectionRewardHandler _rewardHandler;
        private readonly CardCollectionHudPresenter _hudPresenter;
        private readonly CardCollectionInventoryIntegration _inventoryIntegration;
        private readonly ICollectionProgressSnapshotService _snapshotService;

        private CardCollectionRewardsConfigSO _rewardsConfig;

        private CancellationTokenSource _cts;
        private bool _isStarted;
        private bool _isDisposed;

        public CardCollectionSessionContext Context { get; }

        public CardCollectionSession(
            CardCollectionSessionContext context,
            CardCollectionModule module,
            CardCollectionHudPresenter hudPresenter,
            ICardCollectionRewardHandler rewardHandler,
            CardCollectionRewardsConfigSO rewardsConfig,
            CardCollectionInventoryIntegration inventoryIntegration,
            ICollectionProgressSnapshotService snapshotService)
        {
            Context = context;
            _module = module;
            _hudPresenter = hudPresenter;
            _rewardHandler = rewardHandler;
            _rewardsConfig = rewardsConfig;
            _inventoryIntegration = inventoryIntegration;
            _snapshotService = snapshotService;
        }

        public async UniTask StartAsync(ScheduleItem scheduleItem, CancellationToken externalCt)
        {
            ThrowIfDisposed();

            if (_isStarted)
                throw new InvalidOperationException("Session already started");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            var ct = _cts.Token;

            try
            {
                await _module.InitializeAsync(ct);

                var data = await _module.Load(ct);
                _snapshotService.SetSnapshot(data);

                _module.OnGroupCompleted += OnGroupCompleted;
                _module.OnCollectionCompleted += OnCollectionCompleted;

                _inventoryIntegration.Attach();
                _hudPresenter.Bind(scheduleItem, ct);

                _isStarted = true;
            }
            catch
            {
                await SafeStopInternal();
                throw;
            }
        }

        public UniTask UpdateAsync(CancellationToken ct)
        {
            if (!_isStarted || _isDisposed)
                return UniTask.CompletedTask;

            ct.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        public async UniTask StopAsync(CancellationToken externalCt)
        {
            if (!_isStarted || _isDisposed)
                return;

            await SafeStopInternal();
        }

        public UniTask SettleAsync(CancellationToken ct)
        {
            if (_isDisposed)
                return UniTask.CompletedTask;

            return UniTask.CompletedTask;
        }
        
        private async UniTask SafeStopInternal()
        {
            if (!_isStarted)
                return;

            _isStarted = false;

            try
            {
                _cts?.Cancel();
            }
            catch { /* ignore */ }

            _module.OnGroupCompleted -= OnGroupCompleted;
            _module.OnCollectionCompleted -= OnCollectionCompleted;

            try
            {
                _hudPresenter?.Unbind();
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardCollectionRuntime] HUD unbind error: {e}");
            }

            try
            {
                _inventoryIntegration?.Detach();
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardCollectionRuntime] Inventory detach error: {e}");
            }

            try
            {
                _module?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardCollectionRuntime] Module dispose error: {e}");
            }

            try
            {
                _cts?.Dispose();
            }
            catch { /* ignore */ }

            _cts = null;

            await UniTask.CompletedTask;
        }

        private void OnGroupCompleted(CardGroupCompletedData data)
        {
            if (_isDisposed || !_isStarted)
                return;

            var ct = _cts?.Token ?? CancellationToken.None;

            if (ct.IsCancellationRequested)
                return;

            _rewardHandler?
                .TryHandleGroupCompleted(data, ct)
                .Forget();
        }

        private void OnCollectionCompleted(CardCollectionCompletedData data)
        {
            if (_isDisposed || !_isStarted)
                return;

            var ct = _cts?.Token ?? CancellationToken.None;

            if (ct.IsCancellationRequested)
                return;

            _rewardHandler?
                .TryHandleCollectionCompleted(data, ct)
                .Forget();
        }


        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                SafeStopInternal().Forget();
            }
            catch { /* ignore */ }

            try
            {
                (_hudPresenter as IDisposable)?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardCollectionRuntime] HUD dispose error: {e}");
            }

            if (_rewardsConfig != null)
            {
                try
                {
                    AddressablesWrapper.Release(_rewardsConfig);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CardCollectionRuntime] Addressables release error: {e}");
                }

                _rewardsConfig = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CardCollectionSession));
        }
    }
}