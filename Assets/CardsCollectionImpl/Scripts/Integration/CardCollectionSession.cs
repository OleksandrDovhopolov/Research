using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
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

        private CancellationTokenSource _cts;
        public CardCollectionSessionContext Context { get; }

        public CardCollectionSession(
            CardCollectionModule module,
            CardCollectionHudPresenter hudPresenter,
            ICardCollectionRewardHandler rewardHandler,
            CardCollectionInventoryIntegration inventoryIntegration,
            ICollectionProgressSnapshotService  collectionProgressSnapshotService)
        {
            _module = module;
            _hudPresenter = hudPresenter;
            _rewardHandler = rewardHandler;
            _inventoryIntegration = inventoryIntegration;
            _collectionProgressSnapshotService = collectionProgressSnapshotService;
            Context = new CardCollectionSessionContext(_module, _module, _module);
        }
        
        public async UniTask StartAsync(ScheduleItem scheduleItem, CancellationToken externalCt)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            var ct = _cts.Token;

            await _rewardHandler.InitializeAsync(externalCt);
            await _module.InitializeAsync(ct);

            var collectionData = await _module.Load(ct);
            _collectionProgressSnapshotService.SetSnapshot(collectionData);
            
            _module.OnGroupCompleted += OnGroupCompleted;
            _module.OnCollectionCompleted += OnCollectionCompleted;
            
            _inventoryIntegration.AttachAsync(ct);
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
            _inventoryIntegration?.DetachAsync(_cts.Token);

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
            _cts?.Cancel();
            _cts?.Dispose();

            _module?.Dispose();
        }
    }
}