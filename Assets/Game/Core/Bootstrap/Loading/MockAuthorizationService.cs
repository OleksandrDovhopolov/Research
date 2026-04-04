using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Bootstrap.Loading
{
    public sealed class MockAuthorizationService : IAuthorizationService
    {
        private bool _isAuthorized;

        public UniTask<bool> HasCachedTokenAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.FromResult(_isAuthorized);
        }

        public async UniTask<bool> AuthorizeAsync(AuthorizationLoginMethod method, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Delay(250, cancellationToken: ct);
            _isAuthorized = true;
            return true;
        }
    }
}
