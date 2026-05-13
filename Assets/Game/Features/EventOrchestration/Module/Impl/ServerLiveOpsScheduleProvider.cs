using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
using Infrastructure;
using UnityEngine;

namespace EventOrchestration
{
    [Serializable]
    public sealed class LiveOpsScheduleResponse
    {
        public string ServerTimeUtc;
        public List<ScheduleItem> Items;
    }

    public sealed class ServerLiveOpsScheduleProvider : IScheduleProvider
    {
        private const string ScheduleUrl = "liveops/schedule";

        private readonly IWebClient _webClient;
        private readonly IServerTimeSyncTarget _serverTimeSyncTarget;
        private IReadOnlyList<ScheduleItem> _lastValidSnapshot = Array.Empty<ScheduleItem>();

        public ServerLiveOpsScheduleProvider(IWebClient webClient)
            : this(webClient, null)
        {
        }

        public ServerLiveOpsScheduleProvider(IWebClient webClient, IServerTimeSyncTarget serverTimeSyncTarget)
        {
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            _serverTimeSyncTarget = serverTimeSyncTarget;
        }

        public async UniTask<IReadOnlyList<ScheduleItem>> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var response = await _webClient.GetAsync<LiveOpsScheduleResponse>(ScheduleUrl, ct);
                ct.ThrowIfCancellationRequested();

                TryUpdateServerTime(response?.ServerTimeUtc);
                var loadedItems = response?.Items ?? new List<ScheduleItem>();
                var normalizedItems = CloneAndNormalize(loadedItems);
                _lastValidSnapshot = normalizedItems;
                return normalizedItems;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (WebClientException exception)
            {
                Debug.LogError($"[ServerLiveOpsScheduleProvider] Failed to load liveops schedule from server. {exception}");
                return CloneAndNormalize(_lastValidSnapshot);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ServerLiveOpsScheduleProvider] Unexpected error while loading liveops schedule. {exception}");
                return CloneAndNormalize(_lastValidSnapshot);
            }
        }

        private void TryUpdateServerTime(string rawServerTimeUtc)
        {
            if (_serverTimeSyncTarget == null)
            {
                return;
            }

            if (DateTimeOffset.TryParse(
                    rawServerTimeUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var serverTimeUtc))
            {
                _serverTimeSyncTarget.UpdateServerUtcNow(serverTimeUtc);
                return;
            }

            if (_serverTimeSyncTarget.IsSynchronized)
            {
                Debug.LogWarning("[ServerLiveOpsScheduleProvider] liveops schedule response has missing or invalid serverTimeUtc. Keeping previous synchronized orchestration clock baseline.");
                return;
            }

            Debug.LogWarning("[ServerLiveOpsScheduleProvider] liveops schedule response has missing or invalid serverTimeUtc. Orchestration clock is unsynchronized and will use local UTC fallback.");
        }

        private static IReadOnlyList<ScheduleItem> CloneAndNormalize(IReadOnlyList<ScheduleItem> items)
        {
            if (items == null || items.Count == 0)
            {
                return Array.Empty<ScheduleItem>();
            }

            var clone = new List<ScheduleItem>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null)
                {
                    continue;
                }

                clone.Add(new ScheduleItem
                {
                    Id = item.Id,
                    EventType = item.EventType,
                    StartTimeUtc = item.StartTimeUtc,
                    EndTimeUtc = item.EndTimeUtc,
                    Priority = item.Priority,
                    StreamId = item.StreamId,
                    CustomParams = item.CustomParams == null
                        ? new Dictionary<string, string>(StringComparer.Ordinal)
                        : new Dictionary<string, string>(item.CustomParams, StringComparer.Ordinal),
                });
            }

            return clone;
        }
    }
}
