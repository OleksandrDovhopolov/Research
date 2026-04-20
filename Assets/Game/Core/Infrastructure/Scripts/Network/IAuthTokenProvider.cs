using System.Threading;
using Cysharp.Threading.Tasks;

namespace Infrastructure
{
    public interface IAuthTokenProvider
    {
        UniTask<string> GetTokenAsync(CancellationToken ct = default);
    }
}
