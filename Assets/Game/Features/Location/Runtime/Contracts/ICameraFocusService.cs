using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Features.Locations
{
    public interface ICameraFocusService
    {
        Task FocusAsync(
            ILocationInteractable interactable,
            Vector3 fallbackWorldPosition,
            float targetSize = 15f,
            CancellationToken cancellationToken = default);
    }
}
