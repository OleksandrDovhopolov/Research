using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace EventOrchestration.Controllers
{
    public interface ICardCollectionRuntime
    {
        UniTask StartAsync(CardCollectionEventModel model, CancellationToken ct);
        UniTask UpdateAsync(CardCollectionEventModel model, CancellationToken ct);
        UniTask EndAsync(CardCollectionEventModel model, CancellationToken ct);
        UniTask SettleAsync(CardCollectionEventModel model, CancellationToken ct);
    }
}
