using System;
using System.Collections.Generic;
using System.Linq;
using CardCollection.Core;
using UnityEngine;

namespace CardCollectionImpl
{
   public sealed class PendingGroupCompletionPresentationQueue : IPendingGroupCompletionPresentationQueue
    {
        private readonly object _sync = new();
        private readonly List<string> _queue = new();
        private readonly IReadOnlyList<CardCollectionGroupConfig> _groupConfigs;

        public PendingGroupCompletionPresentationQueue(IReadOnlyList<CardCollectionGroupConfig> groupConfigs)
        {
            _groupConfigs = groupConfigs ?? throw new ArgumentNullException(nameof(groupConfigs));
        }

        public void Enqueue(IEnumerable<string> groupTypes)
        {
            if (groupTypes == null)
            {
                return;
            }

            lock (_sync)
            {
                foreach (var groupType in groupTypes.Where(value => !string.IsNullOrWhiteSpace(value)))
                {
                    if (_queue.Contains(groupType))
                    {
                        continue;
                    }

                    _queue.Add(groupType);
                }
            }
        }

        public IReadOnlyList<CardCollectionGroupConfig> DequeueAll()
        {
            lock (_sync)
            {
                if (_queue.Count == 0)
                {
                    return Array.Empty<CardCollectionGroupConfig>();
                }

                var groupTypes = _queue.ToArray();
                _queue.Clear();

                var result = new List<CardCollectionGroupConfig>(groupTypes.Length);
                foreach (var groupType in groupTypes)
                {
                    var groupConfig = _groupConfigs.FirstOrDefault(group => group.groupType == groupType);
                    if (groupConfig == null)
                    {
                        Debug.LogError($"Failed to find group {groupType}");
                        continue;
                    }

                    result.Add(groupConfig);
                }

                return result;
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _queue.Clear();
            }
        }
    }
}