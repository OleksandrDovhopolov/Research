using Cysharp.Threading.Tasks;
using System.Threading;

namespace Infrastructure
{
    public sealed class NoOpAuthTokenProvider : IAuthTokenProvider
    {
        public UniTask<string> GetTokenAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.FromResult<string>(null);
        }
    }
}
