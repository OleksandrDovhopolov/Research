using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Bootstrap.Loading
{
    public sealed class RemoteConfigFetchOperation : LoadingOperationBase
    {
        private readonly RemoteConfigLoader _remoteConfigLoader;

        public RemoteConfigFetchOperation(RemoteConfigLoader remoteConfigLoader)
            : base(
                id: "remote_config_fetch",
                description: "Loading remote configuration",
                isCritical: false,
                weight: 0.2f,
                displayPriority: 85,
                retryPolicy: new LoadingRetryPolicy(2, TimeSpan.FromSeconds(0.5)),
                timeout: TimeSpan.FromSeconds(10))
        {
            _remoteConfigLoader = remoteConfigLoader;
        }

        protected override async UniTask ExecuteInternalAsync(CancellationToken ct)
        {
            ReportProgress(0.1f);
            await _remoteConfigLoader.FetchAndActivateAsync(ct);
            ReportProgress(1f);
        }
    }
}
