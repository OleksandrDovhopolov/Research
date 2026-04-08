using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Bootstrap.Loading
{
    public sealed class FirebaseDependenciesOperation : LoadingOperationBase
    {
        private readonly RemoteConfigLoader _remoteConfigLoader;

        public FirebaseDependenciesOperation(RemoteConfigLoader remoteConfigLoader)
            : base(
                id: "firebase_dependencies",
                description: "Initializing core services",
                isCritical: false,
                weight: 0.1f,
                displayPriority: 100,
                retryPolicy: new LoadingRetryPolicy(2, TimeSpan.FromSeconds(0.5)),
                timeout: TimeSpan.FromSeconds(8))
        {
            _remoteConfigLoader = remoteConfigLoader;
        }

        protected override async UniTask ExecuteInternalAsync(CancellationToken ct)
        {
            ReportProgress(0.1f);
            await _remoteConfigLoader.EnsureDependenciesAsync(ct);
            ReportProgress(1f);
        }
    }
}
