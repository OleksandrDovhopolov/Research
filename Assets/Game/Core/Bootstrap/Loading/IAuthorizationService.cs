using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Bootstrap.Loading
{
    public interface IAuthorizationService
    {
        UniTask<bool> HasCachedTokenAsync(CancellationToken ct);
        UniTask<bool> AuthorizeAsync(AuthorizationLoginMethod method, CancellationToken ct);
    }
}
