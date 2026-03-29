using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using Infrastructure;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardCollectionSession : IDisposable
    {
        private readonly UIManager _uiManager;
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
            UIManager uiManager,
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
            _uiManager = uiManager;
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

                //TODO add collection name
                Debug.LogWarning($"[Debug] Context.CollectionId {Context.CollectionId}");
                var args = new CollectionStartedArgs(Context.CollectionId, "Spring Collection");
                _uiManager.Show<CollectionStartedController>(args, UIShowCommand.UIShowType.Ordered);
                
                _isStarted = true;
            }
            catch
            {
                SafeStopInternal(ct);
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

        public UniTask StopAsync(CancellationToken externalCt)
        {
            if (!_isStarted || _isDisposed)
                return UniTask.CompletedTask;

            externalCt.ThrowIfCancellationRequested();
            HideEventWindows();
            SafeStopInternal(externalCt);
            
            //TODO add collection name
            var args = new CollectionCompletedArgs(Context.CollectionId, "Spring Collection");
            _uiManager.Show<CollectionCompletedController>(args);

            return UniTask.CompletedTask;
        }

        private void HideEventWindows()
        {
            //TODO find better way
            if (_uiManager.IsWindowShown<CardCollectionController>())
            {
                _uiManager.Hide<CardCollectionController>();
            }
            
            if (_uiManager.IsWindowShown<NewCardController>())
            {
                _uiManager.Hide<NewCardController>();
            }
            
            //TODO uncomment this when new window created
            /*if (_uiManager.IsWindowShown<CardGroupRewardController>())
            {
                _uiManager.Hide<CardGroupRewardController>();
            }*/
        }
        
        public UniTask SettleAsync(CancellationToken ct)
        {
            if (_isDisposed)
                return UniTask.CompletedTask;

            return UniTask.CompletedTask;
        }
        
        private void SafeStopInternal(CancellationToken ct)
        {
            if (!_isStarted)
                return;

            /*
             TODO check this hint
             * One Small Bug in your SafeStopInternal:You have ct.ThrowIfCancellationRequested(); inside your cleanup logic.
             * Warning: Usually, you should not throw inside a cleanup/internal stop method.
             * If the externalCt is cancelled, you might skip _hudPresenter.Unbind() or _inventoryIntegration.Detach(),
             * leaving "ghost" event listeners active in your game.
             */
            ct.ThrowIfCancellationRequested();
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
                ct.ThrowIfCancellationRequested();
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
                SafeStopInternal(CancellationToken.None);
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