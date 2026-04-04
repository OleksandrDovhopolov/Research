using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Bootstrap.Loading
{
    public interface IAuthorizationGate
    {
        UniTask WaitUntilAuthorizedAsync(CancellationToken ct);
    }
}
