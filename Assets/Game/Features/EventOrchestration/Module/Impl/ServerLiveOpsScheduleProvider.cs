using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
using Infrastructure;
using UnityEngine;

namespace EventOrchestration
{
    public sealed class ServerLiveOpsScheduleProvider : IScheduleProvider
    {
        private const string ScheduleUrl = "liveops/schedule";

        private readonly IWebClient _webClient;
        private IReadOnlyList<ScheduleItem> _lastValidSnapshot = Array.Empty<ScheduleItem>();

        public ServerLiveOpsScheduleProvider(IWebClient webClient)
        {
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
        }

        public async UniTask<IReadOnlyList<ScheduleItem>> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var loadedItems = await _webClient.GetAsync<List<ScheduleItem>>(ScheduleUrl, ct);
                ct.ThrowIfCancellationRequested();

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
