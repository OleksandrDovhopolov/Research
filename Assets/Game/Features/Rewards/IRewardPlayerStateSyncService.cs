using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public interface IRewardPlayerStateSyncService
    {
        UniTask SyncFromGlobalSaveAsync(CancellationToken ct = default);
    }
}
