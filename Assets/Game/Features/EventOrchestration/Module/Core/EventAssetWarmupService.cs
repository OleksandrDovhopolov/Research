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

        private static readonly TimeSpan PrepareDisk_SecondsBefore = TimeSpan.FromMilliseconds(10000);
        private static readonly TimeSpan PrepareRam_SecondsBefore = TimeSpan.FromMilliseconds(500);
        
        //TODO update If you have 150 cards in your collection, that means the other 140 will load cold when you open the window.
        private const int MaxWarmupSpritesCount = 10;

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
            Debug.Log("[EventAssetWarmupService] Start: subscribed to OnEventCompleted and created lifetime token.");
        }

        public void Tick()
        {
            var ct = _lifetimeCts?.Token ?? CancellationToken.None;
            if (ct.IsCancellationRequested)
            {
                Debug.Log("[EventAssetWarmupService] Tick skipped: cancellation requested.");
                return;
            }

            if (Interlocked.CompareExchange(ref _tickInProgress, 1, 0) != 0)
            {
                Debug.Log("[EventAssetWarmupService] Tick skipped: previous tick is still running.");
                return;
            }

            Debug.Log("[EventAssetWarmupService] Tick started.");
            TickAsync(ct).Forget();
        }

        public void ReleaseAllWarmedAssets()
        {
            Debug.Log($"[EventAssetWarmupService] ReleaseAllWarmedAssets: count={_warmupByEventId.Count}.");
            foreach (var pair in _warmupByEventId)
            {
                ReleaseForEvent(pair.Key, pair.Value);
            }

            _warmupByEventId.Clear();
            Debug.Log("[EventAssetWarmupService] ReleaseAllWarmedAssets: completed.");
        }

        public void Dispose()
        {
            Debug.Log("[EventAssetWarmupService] Dispose: begin.");
            _orchestrator.OnEventCompleted -= HandleEventCompleted;
            ReleaseAllWarmedAssets();

            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
            Debug.Log("[EventAssetWarmupService] Dispose: completed.");
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
                {
                    Debug.Log("[EventAssetWarmupService] TickAsync: no upcoming event.");
                    return;
                }

                var state = GetOrCreateState(item.Id);
                var now = _clock.UtcNow;
                Debug.Log($"[EventAssetWarmupService] TickAsync: eventId={item.Id}, now={now:O}, start={item.StartTimeUtc:O}, diskPrepared={state.DiskPrepared}, ramPrepared={state.RamPrepared}.");

                if (!state.DiskPrepared && now >= item.StartTimeUtc - PrepareDisk_SecondsBefore)
                {
                    Debug.Log($"[EventAssetWarmupService] Disk warmup: start for eventId={item.Id}.");
                    await ProdAddressablesWrapper.DownloadDependenciesByLabelAsync(item.Id, ct);
                    state.DiskPrepared = true;
                    Debug.Log($"[EventAssetWarmupService] Disk warmup: completed for eventId={item.Id}.");
                }

                if (!state.RamPrepared && now >= item.StartTimeUtc - PrepareRam_SecondsBefore)
                {
                    Debug.Log($"[EventAssetWarmupService] RAM warmup: start for eventId={item.Id}, maxCount={MaxWarmupSpritesCount}.");
                    var warmedAddresses = await ProdAddressablesWrapper.WarmupGroupByLabelAsync<Sprite>(
                        item.Id,
                        ct,
                        MaxWarmupSpritesCount);
                    state.WarmedAddresses = warmedAddresses;
                    state.RamPrepared = true;
                    Debug.Log($"[EventAssetWarmupService] RAM warmup: completed for eventId={item.Id}, warmedCount={warmedAddresses.Count}.");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[EventAssetWarmupService] TickAsync canceled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventAssetWarmupService] Warmup tick failed: {ex}");
            }
            finally
            {
                Interlocked.Exchange(ref _tickInProgress, 0);
                Debug.Log("[EventAssetWarmupService] Tick finished.");
            }
        }

        private WarmupState GetOrCreateState(string eventId)
        {
            if (!_warmupByEventId.TryGetValue(eventId, out var state))
            {
                state = new WarmupState();
                _warmupByEventId[eventId] = state;
                Debug.Log($"[EventAssetWarmupService] Created warmup state for eventId={eventId}.");
            }

            return state;
        }

        private void HandleEventCompleted(EventOrchestration.Models.ScheduleItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
            {
                Debug.Log("[EventAssetWarmupService] OnEventCompleted ignored: invalid item.");
                return;
            }

            if (!_warmupByEventId.TryGetValue(item.Id, out var state))
            {
                Debug.Log($"[EventAssetWarmupService] OnEventCompleted: no warmup state for eventId={item.Id}.");
                return;
            }

            Debug.Log($"[EventAssetWarmupService] OnEventCompleted: releasing warmed assets for eventId={item.Id}.");
            ReleaseForEvent(item.Id, state);
            _warmupByEventId.Remove(item.Id);
            Debug.Log($"[EventAssetWarmupService] OnEventCompleted: removed warmup state for eventId={item.Id}.");
        }

        private static void ReleaseForEvent(string eventId, WarmupState state)
        {
            try
            {
                if (state?.WarmedAddresses != null && state.WarmedAddresses.Count > 0)
                {
                    Debug.Log($"[EventAssetWarmupService] ReleaseForEvent: eventId={eventId}, addresses={state.WarmedAddresses.Count}.");
                    ProdAddressablesWrapper.ReleaseGroup(state.WarmedAddresses);
                    Debug.Log($"[EventAssetWarmupService] ReleaseForEvent: completed for eventId={eventId}.");
                }
                else
                {
                    Debug.Log($"[EventAssetWarmupService] ReleaseForEvent: nothing to release for eventId={eventId}.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventAssetWarmupService] Failed to release warmed assets for '{eventId}': {ex}");
            }
        }
    }
}
