using System.Threading;
using Cysharp.Threading.Tasks;

namespace Inventory.API
{
    public interface IOpenable
    {
        UniTask OpenAsync(CancellationToken cancellationToken = default);
    }
}
