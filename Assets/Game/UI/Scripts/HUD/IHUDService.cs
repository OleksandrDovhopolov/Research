using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameplayUI
{
    public interface IHUDService
    {
        UniTask<IEventButton> SpawnEventButtonAsync(string spriteAddress, CancellationToken ct);
        void RemoveEventButton(string eventId);
    }
}