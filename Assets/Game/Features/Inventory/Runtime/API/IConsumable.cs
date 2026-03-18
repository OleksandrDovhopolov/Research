using System.Threading;
using Cysharp.Threading.Tasks;

namespace Inventory.API
{
    public interface IConsumable
    {
        UniTask ConsumeAsync(CancellationToken cancellationToken = default);
    }
}
