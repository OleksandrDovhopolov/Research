using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace EventOrchestration
{
    public sealed class StreamingAssetsLiveOpsScheduleContentSource : ILiveOpsScheduleContentSource
    {
        private readonly string _relativeFilePath;

        public StreamingAssetsLiveOpsScheduleContentSource(string relativeFilePath)
        {
            _relativeFilePath = relativeFilePath ?? throw new ArgumentNullException(nameof(relativeFilePath));
        }

        public async UniTask<string> LoadJsonAsync(CancellationToken ct)
        {
            var fullPath = Path.Combine(Application.streamingAssetsPath, _relativeFilePath);

            if (Application.platform == RuntimePlatform.Android)
            {
                using var request = UnityWebRequest.Get(fullPath);
                await request.SendWebRequest().WithCancellation(ct);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new FileNotFoundException($"Schedule file not found via WebRequest: {fullPath}. Error: {request.error}");
                }

                return request.downloadHandler.text;
            }

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Schedule file not found: {fullPath}");
            }

            return await File.ReadAllTextAsync(fullPath, ct);
        }
    }

    public sealed class JsonScheduleProvider : IScheduleProvider
    {
        private readonly ILiveOpsScheduleContentSource _contentSource;
        private readonly IScheduleValidator _scheduleValidator;

        public JsonScheduleProvider(ILiveOpsScheduleContentSource contentSource, IScheduleValidator scheduleValidator)
        {
            _contentSource = contentSource ?? throw new ArgumentNullException(nameof(contentSource));
            _scheduleValidator = scheduleValidator ?? throw new ArgumentNullException(nameof(scheduleValidator));
        }

        public async UniTask<IReadOnlyList<ScheduleItem>> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var json = await _contentSource.LoadJsonAsync(ct);
            ct.ThrowIfCancellationRequested();

            var items = string.IsNullOrWhiteSpace(json)
                ? new List<ScheduleItem>()
                : JsonConvert.DeserializeObject<List<ScheduleItem>>(json);

            if (items == null)
            {
                throw new InvalidOperationException("Failed to deserialize live ops schedule json.");
            }

            var validationErrors = await _scheduleValidator.ValidateAsync(items, ct);
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException("Schedule validation failed: " + string.Join("; ", validationErrors));
            }

            return items;
        }
    }
}
