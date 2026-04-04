using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;

namespace Game.Bootstrap.Loading.Operations
{
    public sealed class AddressablesUpdateOperation : LoadingOperationBase
    {
        public AddressablesUpdateOperation()
            : base(
                id: "addressables_update",
                description: "Checking game content updates",
                isCritical: true,
                weight: 0.3f,
                displayPriority: 95,
                retryPolicy: new LoadingRetryPolicy(2, TimeSpan.FromSeconds(1)),
                timeout: TimeSpan.FromSeconds(20))
        {
        }

        protected override async UniTask ExecuteInternalAsync(CancellationToken ct)
        {
            await AddressablesUpdater.CheckAndUpdateAsync(
                ct,
                new Progress<float>(value => ReportProgress(value)));
            ReportProgress(1f);
        }
    }
}
