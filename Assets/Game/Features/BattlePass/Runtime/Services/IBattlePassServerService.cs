using System.Threading;
using Cysharp.Threading.Tasks;

namespace BattlePass
{
    public interface IBattlePassServerService
    {
        UniTask<BattlePassSnapshot> GetCurrentAsync(CancellationToken ct = default);
    }
}
