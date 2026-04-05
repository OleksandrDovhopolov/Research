using System.Threading;
using Cysharp.Threading.Tasks;

namespace UIShared
{
    public interface IHUDService
    {
        UniTask<IEventButton> SpawnEventButtonAsync(string spriteAddress, CancellationToken ct);
        void RemoveEventButton(string eventId);
    }
}