using System.Threading;
using Cysharp.Threading.Tasks;

namespace BattlePass
{
    public interface IBattlePassStartupSync
    {
        UniTask InitializeAsync(CancellationToken ct);
    }
}
