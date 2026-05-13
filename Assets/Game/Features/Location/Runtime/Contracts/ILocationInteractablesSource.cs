using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Features.Locations
{
    public interface ILocationInteractablesSource
    {
        IReadOnlyList<ILocationInteractable> InteractionObjects { get; }

        UniTask WaitForLocationAsync(CancellationToken cancellationToken);
    }
}
