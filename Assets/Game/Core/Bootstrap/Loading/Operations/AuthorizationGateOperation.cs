using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Bootstrap.Loading
{
    public sealed class AuthorizationGateOperation : LoadingOperationBase
    {
        private readonly IAuthorizationGate _authorizationGate;

        public AuthorizationGateOperation(IAuthorizationGate authorizationGate)
            : base(
                id: "authorization_gate",
                description: "Authorizing player",
                isCritical: true,
                weight: 0.15f,
                displayPriority: 100,
                retryPolicy: new LoadingRetryPolicy(1, TimeSpan.Zero),
                timeout: TimeSpan.FromMinutes(5))
        {
            _authorizationGate = authorizationGate;
        }

        protected override async UniTask ExecuteInternalAsync(CancellationToken ct)
        {
            ReportProgress(0.1f);
            await _authorizationGate.WaitUntilAuthorizedAsync(ct);
            ReportProgress(1f);
        }
    }
}
