using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using UIShared;
using UnityEngine;
using VContainer;

namespace EventOrchestration
{
    public class GlobalTimerService : MonoBehaviour, IGlobalTimerService
    {
        private readonly Dictionary<string, DateTimeOffset> _events = new();
        private IClock _clock;
        private CancellationToken _destroyCt;
        private bool _heartbeatActive;
        public event Action<string, TimeSpan> OnTick;
        public event Action<string> OnTimerFinished;
        
        [Inject]
        private void Construct(IClock clock)
        {
            _clock = clock;
        }
        
        private void Awake()
        {
            _destroyCt = this.GetCancellationTokenOnDestroy();
        }
        
        public void Register(string eventId, DateTimeOffset timeUtc)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event id cannot be null or empty.", nameof(eventId));

            if (_events.ContainsKey(eventId))
            {
                _events[eventId] = timeUtc;
            }
            else
            {
                var wasEmpty = _events.Count == 0;
                _events[eventId] = timeUtc;
                
                if (!wasEmpty || _heartbeatActive) return;
                _heartbeatActive = true;
                RunHeartbeatAsync(_destroyCt).Forget();
            }
        }
        
        public void Unregister(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                return;
            _events.Remove(eventId);
        }
        
        public bool TryGetRemaining(string eventId, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            if (string.IsNullOrEmpty(eventId) || !_events.TryGetValue(eventId, out var endUtc))
                return false;

            var r = endUtc - _clock.UtcNow;
            
            remaining = r.TotalSeconds <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(Math.Ceiling(r.TotalSeconds));
            
            return true;
        }
        
        private async UniTaskVoid RunHeartbeatAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _events.Count > 0)
                {
                    ProcessTick();
                    if (_events.Count == 0)
                        break;
                    await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _heartbeatActive = false;
            }
        }
        
        private void ProcessTick()
        {
            var now = _clock.UtcNow;
            var snapshot = new List<KeyValuePair<string, DateTimeOffset>>(_events);
            var finished = new List<string>();
            foreach (var kv in snapshot)
            {
                if (!_events.TryGetValue(kv.Key, out var endUtc) || endUtc != kv.Value)
                    continue;
                var remaining = endUtc - now;
                if (remaining.TotalSeconds <= 0)
                {
                    OnTimerFinished?.Invoke(kv.Key);
                    finished.Add(kv.Key);
                }
                else
                {
                    OnTick?.Invoke(kv.Key, remaining);
                }
            }
            foreach (var id in finished)
                _events.Remove(id);
        }
    }
}