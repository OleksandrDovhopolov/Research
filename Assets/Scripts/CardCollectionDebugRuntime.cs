using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Controllers;
using EventOrchestration.Models;
using UnityEngine;

namespace core
{
    public sealed class CardCollectionDebugRuntime : ICardCollectionRuntime
    {
        public UniTask StartAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.Log($"[CardCollectionRuntime] Start: {model.EventId}, collection={model.CollectionId}");
            return UniTask.CompletedTask;
        }

        public UniTask UpdateAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        public UniTask EndAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.Log($"[CardCollectionRuntime] End: {model.EventId}");
            return UniTask.CompletedTask;
        }

        public UniTask SettleAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.Log($"[CardCollectionRuntime] Settle: {model.EventId}");
            return UniTask.CompletedTask;
        }
    }
}
