using System.Threading;
using Cysharp.Threading.Tasks;

namespace HUD
{
    public interface IHUDService
    {
        UniTask<IEventButton> SpawnEventButtonAsync(string spriteAddress, CancellationToken ct);
        void RemoveEventButton(string eventId);
    }
}