using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIShared.Loading;
using UnityEngine;

namespace Game.Bootstrap.Loading
{
    public sealed class LoadingAuthorizationGate : IAuthorizationGate, IDisposable
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ILoadingScreenView _loadingScreenView;

        public LoadingAuthorizationGate(IAuthorizationService authorizationService, ILoadingScreenView loadingScreenView)
        {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _loadingScreenView = loadingScreenView ?? throw new ArgumentNullException(nameof(loadingScreenView));
        }

        public async UniTask WaitUntilAuthorizedAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (await _authorizationService.HasCachedTokenAsync(ct))
            {
                await _loadingScreenView.SetLoginButtonsVisibleAsync(false, 0.1f, ct);
                return;
            }

            await _loadingScreenView.SetLoginButtonsVisibleAsync(true, 0.25f, ct);

            while (!ct.IsCancellationRequested)
            {
                LoginSelectionMethod? selectedMethod = await _loadingScreenView.WaitForLoginSelectionAsync(ct);
                if (!selectedMethod.HasValue)
                {
                    continue;
                }

                var authMethod = selectedMethod.Value == LoginSelectionMethod.Facebook
                    ? AuthorizationLoginMethod.Facebook
                    : AuthorizationLoginMethod.Guest;

                var isAuthorized = await _authorizationService.AuthorizeAsync(authMethod, ct);
                if (!isAuthorized)
                {
                    Debug.LogWarning("[LoadingAuth] Authorization failed. Waiting for another selection.");
                    continue;
                }

                await _loadingScreenView.SetLoginButtonsVisibleAsync(false, 0.25f, ct);
                return;
            }

            ct.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
        }
    }
}
