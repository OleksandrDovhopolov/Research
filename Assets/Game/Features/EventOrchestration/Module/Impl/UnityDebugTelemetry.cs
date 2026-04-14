using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
using UnityEngine;

namespace EventOrchestration
{
    public sealed class UnityDebugTelemetry : IOrchestratorTelemetry
    {
        public UniTask TrackTransitionAsync(string scheduleItemId, EventInstanceState from, EventInstanceState to, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.Log($"[EventOrchestrator] {scheduleItemId}: {from} -> {to}");
            return UniTask.CompletedTask;
        }

        public UniTask TrackStartRejectedAsync(string scheduleItemId, string reason, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning($"[EventOrchestrator] Start rejected for {scheduleItemId}. Reason: {reason}");
            return UniTask.CompletedTask;
        }

        public UniTask TrackFailureAsync(string scheduleItemId, string stage, Exception ex, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogError($"[EventOrchestrator] Failure for {scheduleItemId} at {stage}: {ex}");
            return UniTask.CompletedTask;
        }
    }
}
