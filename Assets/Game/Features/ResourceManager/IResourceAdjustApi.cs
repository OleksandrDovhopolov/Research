using System.Threading;
using Cysharp.Threading.Tasks;

namespace CoreResources
{
    public interface IResourceAdjustApi
    {
        UniTask<AdjustResourceResponse> AdjustAsync(AdjustResourceCommand command, CancellationToken ct);
    }
}
