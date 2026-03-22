using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using Infrastructure;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionSession : IDisposable
    {
        private readonly CardCollectionModule _module;
        private readonly ICardCollectionRewardHandler _rewardHandler;
        private readonly CardCollectionHudPresenter _hudPresenter;
        private readonly CardCollectionInventoryIntegration _inventoryIntegration;
        private readonly ICollectionProgressSnapshotService _collectionProgressSnapshotService;
        
        private CardCollectionRewardsConfigSO _rewardsConfig;

        private CancellationTokenSource _cts;
        public CardCollectionSessionContext Context { get; }

        public CardCollectionSession(
            CardCollectionSessionContext sessionContext,
            CardCollectionModule module,
            CardCollectionHudPresenter hudPresenter,
            ICardCollectionRewardHandler rewardHandler,
            CardCollectionRewardsConfigSO rewardsConfig,
            CardCollectionInventoryIntegration inventoryIntegration,
            ICollectionProgressSnapshotService collectionProgressSnapshotService)
        {
            Context = sessionContext;
            _module = module;
            _hudPresenter = hudPresenter;
            _rewardHandler = rewardHandler;
            _rewardsConfig = rewardsConfig;
            _inventoryIntegration = inventoryIntegration;
            _collectionProgressSnapshotService = collectionProgressSnapshotService;
        }
        
        public async UniTask StartAsync(ScheduleItem scheduleItem, CancellationToken externalCt)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            var ct = _cts.Token;

            await _module.InitializeAsync(ct);

            var collectionData = await _module.Load(ct);
            _collectionProgressSnapshotService.SetSnapshot(collectionData);
            
            _module.OnGroupCompleted += OnGroupCompleted;
            _module.OnCollectionCompleted += OnCollectionCompleted;
            
            _inventoryIntegration.Attach();
            _hudPresenter.Bind(scheduleItem, ct);
        }

        public UniTask UpdateAsync(CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }
        
        public async UniTask StopAsync(CancellationToken externalCt)
        {
            if (_cts == null)
                return;

            _cts.Cancel();

            _module.OnGroupCompleted -= OnGroupCompleted;
            _module.OnCollectionCompleted -= OnCollectionCompleted;

            _hudPresenter?.Unbind();
            _inventoryIntegration?.Detach();

            _module.Dispose();

            _cts.Dispose();
            _cts = null;

            await UniTask.CompletedTask;
        }

        public UniTask SettleAsync(CancellationToken ct)
        {
            Debug.Log("[CardCollectionRuntime] Settlement");
            return UniTask.CompletedTask;
        }
        
        private void OnGroupCompleted(CardGroupCompletedData data)
        {
            if (_rewardHandler == null || _cts == null || _cts.IsCancellationRequested)
                return;

            _rewardHandler.TryHandleGroupCompleted(data, _cts.Token).Forget();
        }

        private void OnCollectionCompleted(CardCollectionCompletedData data)
        {
            if (_rewardHandler == null || _cts == null || _cts.IsCancellationRequested)
                return;

            _rewardHandler.TryHandleCollectionCompleted(data, _cts.Token).Forget();
        }

        public void Dispose()
        {
            try
            {
                _cts?.Cancel();
            }
            catch { /* ignore */ }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }

            _module.OnGroupCompleted -= OnGroupCompleted;
            _module.OnCollectionCompleted -= OnCollectionCompleted;

            _hudPresenter?.Unbind();
            _inventoryIntegration?.Detach();
            
            (_hudPresenter as IDisposable)?.Dispose();
            
            if (_rewardsConfig != null)
            {
                AddressablesWrapper.Release(_rewardsConfig);
                _rewardsConfig = null;
            }
            
            _module?.Dispose();
        }
    }
}