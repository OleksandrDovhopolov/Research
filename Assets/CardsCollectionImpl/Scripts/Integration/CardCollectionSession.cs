using System;
using System.Linq;
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
        private readonly ICardCollectionApplicationFacade _facade;
        private readonly ICardCollectionRewardHandler _rewardHandler;
        private readonly CardCollectionHudPresenter _hudPresenter;
        private readonly CardCollectionInventoryIntegration _inventoryIntegration;

        private CardCollectionEventModel _cardCollectionEventModel;

        private CancellationTokenSource _cts;
        private bool _isStarted;
        private bool _isDisposed;

        public CardCollectionSessionContext Context { get; }

        public CardCollectionSession(
            UIManager uiManager,
            CardCollectionSessionContext context,
            ICardCollectionApplicationFacade facade,
            CardCollectionHudPresenter hudPresenter,
            ICardCollectionRewardHandler rewardHandler,
            CardCollectionInventoryIntegration inventoryIntegration)
        {
            Context = context;
            _facade = facade;
            _uiManager = uiManager;
            _hudPresenter = hudPresenter;
            _rewardHandler = rewardHandler;
            _inventoryIntegration = inventoryIntegration;
        }

        public async UniTask StartAsync(CardCollectionEventModel model, ScheduleItem scheduleItem, CancellationToken externalCt)
        {
            ThrowIfDisposed();

            if (_isStarted)
                throw new InvalidOperationException("Session already started");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            var ct = _cts.Token;

            _cardCollectionEventModel = model;
            
            try
            {
                _facade.OnGroupCompleted += OnGroupCompleted;
                _facade.OnCollectionCompleted += OnCollectionCompleted;

                _inventoryIntegration.Attach();
                _hudPresenter.Bind(scheduleItem, ct);

                var args = new CollectionStartedArgs(_cardCollectionEventModel.EventId, _cardCollectionEventModel.CollectionName);
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
            
            var args = new CollectionCompletedArgs(_cardCollectionEventModel.EventId, _cardCollectionEventModel.CollectionName);
            _uiManager.Show<CollectionCompletedController>(args);

            return UniTask.CompletedTask;
        }

        private void HideEventWindows()
        {
            if (_uiManager.IsWindowSpawned<CardCollectionController>())
            {
                var window = _uiManager.GetWindowSync<CardCollectionController>();
                if (window.IsShown)
                {
                    _uiManager.Hide<CardCollectionController>();
                }
            }
            
            if (_uiManager.IsWindowSpawned<NewCardController>())
            {
                var window = _uiManager.GetWindowSync<NewCardController>();
                      
                if (window.IsShown)
                {
                    _uiManager.Hide<NewCardController>();
                }
            }
            
            if (_uiManager.IsWindowSpawned<CollectionStartedController>())
            {
                var window = _uiManager.GetWindowSync<CollectionStartedController>();
                if (window.IsShown)
                {
                    _uiManager.Hide<CollectionStartedController>(true);
                }
            }
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

            await Context.WindowOpener.OpenCardGroupCompletedWindow(groupTypes, ct);
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
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CardCollectionSession));
        }
    }
}