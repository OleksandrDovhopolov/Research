using System;
using System.Collections.Generic;
using System.Linq;
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
        private const int SessionWarmupSpritesCount = 30;
        
        private readonly ICardCollectionApplicationFacade _facade;
        private readonly ICardCollectionRewardHandler _rewardHandler;
        private readonly CardCollectionHudPresenter _hudPresenter;
        private readonly CardCollectionInventoryIntegration _inventoryIntegration;
        private readonly CardCollectionStaticData _eventStaticData;
        private readonly ICollectionProgressSnapshotBuilder _collectionProgressSnapshotBuilder;
        private readonly IExchangeOfferProvider _exchangeOfferProvider;

        private CardCollectionEventModel _cardCollectionEventModel;

        private bool _isStarted;
        private bool _isDisposed;
        private CancellationTokenSource _cts;
        private IReadOnlyList<string> _sessionWarmedAddresses = Array.Empty<string>();

        public CardCollectionSessionContext Context { get; }

        public CardCollectionSession(
            CardCollectionSessionContext context,
            ICardCollectionApplicationFacade facade,
            CardCollectionHudPresenter hudPresenter,
            ICardCollectionRewardHandler rewardHandler,
            CardCollectionInventoryIntegration inventoryIntegration,
            CardCollectionStaticData eventStaticData,
            ICollectionProgressSnapshotBuilder collectionProgressSnapshotBuilder,
            IExchangeOfferProvider exchangeOfferProvider)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
            _hudPresenter = hudPresenter ?? throw new ArgumentNullException(nameof(hudPresenter));
            _rewardHandler = rewardHandler ?? throw new ArgumentNullException(nameof(rewardHandler));
            _inventoryIntegration = inventoryIntegration ?? throw new ArgumentNullException(nameof(inventoryIntegration));
            _eventStaticData = eventStaticData ?? throw new ArgumentNullException(nameof(eventStaticData));
            _collectionProgressSnapshotBuilder = collectionProgressSnapshotBuilder ?? throw new ArgumentNullException(nameof(collectionProgressSnapshotBuilder));
            _exchangeOfferProvider = exchangeOfferProvider ?? throw new ArgumentNullException(nameof(exchangeOfferProvider));

            _hudPresenter.SetShowCollectionHandler(ShowCollectionAsync);
        }

        public async UniTask StartAsync(CardCollectionEventModel model, ScheduleItem scheduleItem, bool firstStart, CancellationToken externalCt)
        {
            ThrowIfDisposed();

            if (_isStarted)
                throw new InvalidOperationException("Session already started");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            var ct = _cts.Token;

            _cardCollectionEventModel = model;
            
            //Debug.LogWarning($"[Debug] CardCollectionSession StartAsync");
            try
            {
                _facade.OnGroupCompleted += OnGroupCompleted;
                _facade.OnCollectionCompleted += OnCollectionCompleted;
                _inventoryIntegration.Attach();

                await EnsureEventAssetsReadyAsync(scheduleItem, ct);
                await ShowStartedAsync(model.EventId, firstStart, ct);
                await _hudPresenter.Bind(scheduleItem, ct);
                
                _isStarted = true;
            }
            catch
            {
                SafeStopInternal(ct);
                throw;
            }
        }

        private async UniTask ShowStartedAsync(string eventId, bool firstStart, CancellationToken ct)
        {
            if (firstStart)
            {
                var spriteAddress = eventId + "/" + CardCollectionGeneralConfig.CollectionBackground;
                var previewSprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(spriteAddress, ct);
                var args = new CollectionStartedArgs(_cardCollectionEventModel.EventId, _cardCollectionEventModel.CollectionName, previewSprite);
                Context.WindowCoordinator.ShowStarted(args);
            }
        }
        
        private async UniTask EnsureEventAssetsReadyAsync(ScheduleItem scheduleItem, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (scheduleItem == null || string.IsNullOrWhiteSpace(scheduleItem.Id))
                throw new ArgumentException("Schedule item is null or invalid.", nameof(scheduleItem));

            await ProdAddressablesWrapper.DownloadDependenciesByLabelAsync(scheduleItem.Id, ct);
            ct.ThrowIfCancellationRequested();

            _sessionWarmedAddresses = await ProdAddressablesWrapper.WarmupGroupByLabelAsync<Sprite>(scheduleItem.Id, ct, SessionWarmupSpritesCount) ?? Array.Empty<string>();
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
            Context.WindowCoordinator.CloseSessionWindows();
            SafeStopInternal(externalCt);
            
            var args = new CollectionCompletedArgs(_cardCollectionEventModel.EventId, _cardCollectionEventModel.CollectionName);
            Context.WindowCoordinator.ShowCompleted(args);

            return UniTask.CompletedTask;
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

            _isStarted = false;

            try
            {
                _cts?.Cancel();
            }
            catch { /* ignore */ }

            _facade.OnGroupCompleted -= OnGroupCompleted;
            _facade.OnCollectionCompleted -= OnCollectionCompleted;

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
                _facade?.Dispose();
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

        private void OnGroupCompleted(CardGroupsCompletedData data)
        {
            if (_isDisposed || !_isStarted)
                return;

            var ct = _cts?.Token ?? CancellationToken.None;

            if (ct.IsCancellationRequested)
                return;

            HandleGroupCompletedAsync(data, ct).Forget();
        }
        
        private async UniTask HandleGroupCompletedAsync(CardGroupsCompletedData data, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (data.Groups == null || data.Groups.Count == 0)
            {
                return;
            }

            var rewardTasks = data.Groups
                .Select(group => _rewardHandler.TryHandleGroupCompleted(group, ct))
                .ToArray();

            var results = await UniTask.WhenAll(rewardTasks);
            var allRewardsGranted = results.All(isGranted => isGranted);
            if (!allRewardsGranted)
            {
                return;
            }

            var groupTypes = data.Groups
                .Select(group => group.GroupType)
                .Where(groupType => !string.IsNullOrWhiteSpace(groupType))
                .Distinct()
                .ToArray();

            if (groupTypes.Length == 0)
            {
                return;
            }

            var groupConfigs = ResolveGroupConfigs(groupTypes);
            if (groupConfigs.Count == 0)
            {
                return;
            }

            var collectionData = await Context.Module.Load(ct);
            var args = new CardGroupCollectionArgs(Context.Module.EventId, collectionData, groupConfigs, _rewardHandler);
            Context.WindowCoordinator.ShowGroupCompleted(args);
        }

        private void OnCollectionCompleted(CardCollectionCompletedData data)
        {
            if (_isDisposed || !_isStarted)
                return;

            var ct = _cts?.Token ?? CancellationToken.None;

            if (ct.IsCancellationRequested)
                return;

            HandleCollectionCompletedAsync(data, ct).Forget();
        }
        
        private async UniTask HandleCollectionCompletedAsync(CardCollectionCompletedData data, CancellationToken ct)
        {
            var granted = await _rewardHandler.TryHandleCollectionCompleted(data, ct);
            if (!granted) return;

            //TODO add window collection reward. no reward animation now is shown
        }

        private async UniTask ShowCollectionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var beforeResetData = await Context.Module.Load(ct);
            var snapshotBeforeReset = _collectionProgressSnapshotBuilder.Build(beforeResetData);

            var newCardsData = CardCollectionNewCardsDto.Create(beforeResetData, _eventStaticData.Cards);
            var newCardIds = newCardsData.NewCardIds;
            if (newCardIds.Count > 0)
            {
                await Context.Module.ResetNewFlagsAsync(newCardIds, ct);
            }

            var afterResetData = await Context.Module.Load(ct);
            var args = new CardCollectionArgs(
                newCardsData,
                afterResetData,
                _exchangeOfferProvider,
                _rewardHandler,
                Context.PointsAccount,
                snapshotBeforeReset,
                Context.Module.EventId,
                _eventStaticData.Cards,
                _eventStaticData.Groups);
            Context.WindowCoordinator.ShowCollection(args);
        }

        private List<CardCollectionGroupConfig> ResolveGroupConfigs(IEnumerable<string> groupTypes)
        {
            var groupConfigs = new List<CardCollectionGroupConfig>();

            foreach (var groupType in groupTypes)
            {
                var groupConfig = _eventStaticData.Groups.FirstOrDefault(group => group.groupType == groupType);
                if (groupConfig == null)
                {
                    Debug.LogError($"Failed to find group {groupType}");
                    continue;
                }

                groupConfigs.Add(groupConfig);
            }

            return groupConfigs;
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
            
            if (_sessionWarmedAddresses is { Count: > 0 })
            {
                ProdAddressablesWrapper.ReleaseGroup(_sessionWarmedAddresses);
                _sessionWarmedAddresses = Array.Empty<string>();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CardCollectionSession));
        }
    }
}