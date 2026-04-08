using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
using Firebase.RemoteConfig;
using Newtonsoft.Json;
using UnityEngine;

namespace EventOrchestration
{
    public sealed class FirebaseRemoteScheduleProvider : IScheduleProvider
    {
        private readonly string _remoteConfigKey;

        public FirebaseRemoteScheduleProvider(string remoteConfigKey)
        {
            if (string.IsNullOrWhiteSpace(remoteConfigKey))
                throw new ArgumentException("Remote Config key cannot be null or empty.", nameof(remoteConfigKey));

            _remoteConfigKey = remoteConfigKey;
        }

        public UniTask<IReadOnlyList<ScheduleItem>> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning($"[Debug] FirebaseRemoteScheduleProvider.LoadAsync start key={_remoteConfigKey}");

            var value = FirebaseRemoteConfig.DefaultInstance.GetValue(_remoteConfigKey);
            var json = value.StringValue;
            Debug.LogWarning($"[Debug] FirebaseRemoteScheduleProvider.LoadAsync json length={json?.Length ?? -1}");

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("[Debug] FirebaseRemoteScheduleProvider.LoadAsync json is empty");
                return UniTask.FromResult<IReadOnlyList<ScheduleItem>>(Array.Empty<ScheduleItem>());
            }

            try
            {
                var dtos = JsonConvert.DeserializeObject<List<ScheduleItemDto>>(json) ?? new List<ScheduleItemDto>();
                var items = dtos
                    .Select(MapToModelSafe)
                    .Where(x => x != null)
                    .ToList()
                    .AsReadOnly();
                Debug.LogWarning($"[Debug] FirebaseRemoteScheduleProvider.LoadAsync parsed items count={items.Count}");

                return UniTask.FromResult<IReadOnlyList<ScheduleItem>>(items);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Debug] FirebaseRemoteScheduleProvider.LoadAsync failed: {ex.Message}");
                throw new InvalidOperationException($"Failed to parse schedule from Remote Config key '{_remoteConfigKey}': {ex.Message}", ex);
            }
        }

        private static ScheduleItem MapToModelSafe(ScheduleItemDto dto)
        {
            if (dto == null) return null;
            if (string.IsNullOrWhiteSpace(dto.id)) return null;

            var start = ParseTime(dto.startTimeUtc);
            var end = ParseTime(dto.endTimeUtc);
            if (end <= start) return null;

            var priority = dto.priority;
            var streamId = string.IsNullOrWhiteSpace(dto.streamId) ? "default" : dto.streamId;
            var eventType = string.IsNullOrWhiteSpace(dto.eventType) ? "default" : dto.eventType;
            var customParams = dto.customParams ?? new Dictionary<string, string>();

            return new ScheduleItem
            {
                Id = dto.id,
                StreamId = streamId,
                StartTimeUtc = start,
                EndTimeUtc = end,
                Priority = priority,
                EventType = eventType,
                CustomParams = new Dictionary<string, string>(customParams, StringComparer.Ordinal)
            };
        }

        private static DateTimeOffset ParseTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException("Time value is null or empty.");

            // Try epoch milliseconds
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epochMs))
            {
                // Heuristic: treat >= 10^12 as ms, >= 10^9 as seconds
                if (epochMs >= 1_000_000_000_000L) // ms
                    return DateTimeOffset.FromUnixTimeMilliseconds(epochMs);
                if (epochMs >= 1_000_000_000L) // seconds
                    return DateTimeOffset.FromUnixTimeSeconds(epochMs);
                // Fallback: assume seconds
                return DateTimeOffset.FromUnixTimeSeconds(epochMs);
            }

            // Try ISO-8601 or RFC 3339
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
            {
                return dto;
            }

            throw new FormatException($"Unrecognized time format: '{value}'");
        }

        private sealed class ScheduleItemDto
        {
            public string id { get; set; }
            public string streamId { get; set; }
            public string startTimeUtc { get; set; }
            public string endTimeUtc { get; set; }
            public int priority { get; set; }
            public string eventType { get; set; }
            public Dictionary<string, string> customParams { get; set; }
        }
    }
}