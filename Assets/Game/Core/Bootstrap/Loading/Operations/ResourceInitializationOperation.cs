using System;
using System.Threading;
using CoreResources;
using Cysharp.Threading.Tasks;

namespace Game.Bootstrap.Loading.Operations
{
    public sealed class ResourceInitializationOperation : LoadingOperationBase
    {
        private readonly ResourceManager _resourceManager;

        public ResourceInitializationOperation(ResourceManager resourceManager)
            : base(
                id: "resources_init",
                description: "Initializing player resources",
                isCritical: true,
                weight: 0.3f,
                displayPriority: 80,
                retryPolicy: new LoadingRetryPolicy(2, TimeSpan.FromSeconds(0.3)),
                timeout: TimeSpan.FromSeconds(12))
        {
            _resourceManager = resourceManager;
        }

        protected override async UniTask ExecuteInternalAsync(CancellationToken ct)
        {
            ReportProgress(0.1f);
            await _resourceManager.InitializeAsync(ct);
            ReportProgress(1f);
        }
    }
}
