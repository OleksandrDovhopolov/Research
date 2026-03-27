using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace core
{
    //TODO remove relativeFilePath from steaming assets
    public sealed class StreamingAssetsScheduleProvider : IScheduleProvider
    {
        private readonly string _relativeFilePath;

        public StreamingAssetsScheduleProvider(string relativeFilePath)
        {
            _relativeFilePath = relativeFilePath ?? throw new ArgumentNullException(nameof(relativeFilePath));
        }

        public async UniTask<IReadOnlyList<ScheduleItem>> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var fullPath = Path.Combine(Application.streamingAssetsPath, _relativeFilePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Schedule file not found: {fullPath}");
            }

            var json = await File.ReadAllTextAsync(fullPath, ct);
            ct.ThrowIfCancellationRequested();

            var items = JsonConvert.DeserializeObject<List<ScheduleItem>>(json);
            if (items == null)
            {
                throw new InvalidOperationException($"Failed to deserialize schedule json from '{fullPath}'.");
            }

            return items;
        }
    }
}
