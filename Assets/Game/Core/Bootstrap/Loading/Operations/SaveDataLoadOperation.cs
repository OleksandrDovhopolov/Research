using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure.SaveSystem;

namespace Game.Bootstrap.Loading.Operations
{
    public sealed class SaveDataLoadOperation : LoadingOperationBase
    {
        private readonly SaveService _saveService;

        public SaveDataLoadOperation(SaveService saveService)
            : base(
                id: "save_data_load",
                description: "Loading save data",
                isCritical: true,
                weight: 0.3f,
                displayPriority: 90,
                retryPolicy: new LoadingRetryPolicy(2, TimeSpan.FromSeconds(0.3)),
                timeout: TimeSpan.FromSeconds(10))
        {
            _saveService = saveService;
        }

        protected override async UniTask ExecuteInternalAsync(CancellationToken ct)
        {
            ReportProgress(0.1f);
            await _saveService.LoadAllAsync(ct);
            ReportProgress(1f);
        }
    }
}
