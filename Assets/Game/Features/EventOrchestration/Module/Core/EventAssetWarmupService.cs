using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using Infrastructure;
using UnityEngine;
using VContainer.Unity;

namespace EventOrchestration.Core
{
    public sealed class EventAssetWarmupService : IEventAssetWarmupService, IStartable, ITickable, IDisposable
    {
        private sealed class WarmupState
        {
            public bool DiskPrepared;
            public bool RamPrepared;
            public IReadOnlyList<string> WarmedAddresses;
        }

        private static readonly TimeSpan PrepareDisk_SecondsBefore = TimeSpan.FromMilliseconds(7000);
        private static readonly TimeSpan PrepareRam_SecondsBefore = TimeSpan.FromMilliseconds(3000);
        
        //TODO update If you have 150 cards in your collection, that means the other 140 will load cold when you open the window.
        private const int MaxWarmupSpritesCount = 30;

        private readonly EventOrchestrator _orchestrator;
        private readonly IClock _clock;
        private readonly Dictionary<string, WarmupState> _warmupByEventId = new();

        private CancellationTokenSource _lifetimeCts;
        //TODO Tick blocked by one async process. other events do not processed 
        private int _tickInProgress;

        public EventAssetWarmupService(EventOrchestrator orchestrator, IClock clock)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void Start()
        {
            _lifetimeCts = new CancellationTokenSource();
            _orchestrator.OnEventCompleted += HandleEventCompleted;
        }

        public void Tick()
        {
            var ct = _lifetimeCts?.Token ?? CancellationToken.None;
            if (ct.IsCancellationRequested)
                return;

            if (Interlocked.CompareExchange(ref _tickInProgress, 1, 0) != 0)
                return;
            
            TickAsync(ct).Forget();
        }

        public void ReleaseAllWarmedAssets()
        {
            foreach (var pair in _warmupByEventId)
            {
                ReleaseForEvent(pair.Key, pair.Value);
            }

            _warmupByEventId.Clear();
        }

        public void Dispose()
        {
            _orchestrator.OnEventCompleted -= HandleEventCompleted;
            ReleaseAllWarmedAssets();

            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
        }

        private async UniTaskVoid TickAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                //TODO Problem: If two events are scheduled to start within 5 seconds of each other,
                //the second one may only start to warm up once the first one has already started.
                var item = _orchestrator.GetNextUpcomingEvent();
                if (item == null || string.IsNullOrWhiteSpace(item.Id))
                    return;

                var state = GetOrCreateState(item.Id);
                var now = _clock.UtcNow;

                if (!state.DiskPrepared && now >= item.StartTimeUtc - PrepareDisk_SecondsBefore)
                {
                    await ProdAddressablesWrapper.DownloadDependenciesByLabelAsync(item.Id, ct);
                    state.DiskPrepared = true;
                }

                if (!state.RamPrepared && now >= item.StartTimeUtc - PrepareRam_SecondsBefore)
                {
                    var warmedAddresses = await ProdAddressablesWrapper.WarmupGroupByLabelAsync<Sprite>(
                        item.Id,
                        ct,
                        MaxWarmupSpritesCount);
                    state.WarmedAddresses = warmedAddresses;
                    state.RamPrepared = true;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during disposal/shutdown.
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventAssetWarmupService] Warmup tick failed: {ex}");
            }
            finally
            {
                Interlocked.Exchange(ref _tickInProgress, 0);
            }
        }

        private WarmupState GetOrCreateState(string eventId)
        {
            if (!_warmupByEventId.TryGetValue(eventId, out var state))
            {
                state = new WarmupState();
                _warmupByEventId[eventId] = state;
            }

            return state;
        }

        private void HandleEventCompleted(Models.ScheduleItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
                return;

            if (!_warmupByEventId.TryGetValue(item.Id, out var state))
                return;
            
            ReleaseForEvent(item.Id, state);
            _warmupByEventId.Remove(item.Id);
        }

        private static void ReleaseForEvent(string eventId, WarmupState state)
        {
            try
            {
                if (state?.WarmedAddresses != null && state.WarmedAddresses.Count > 0)
                {
                    ProdAddressablesWrapper.ReleaseGroup(state.WarmedAddresses);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventAssetWarmupService] Failed to release warmed assets for '{eventId}': {ex}");
            }
        }
    }
}
