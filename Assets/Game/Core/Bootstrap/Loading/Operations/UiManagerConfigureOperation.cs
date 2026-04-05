using System;
using System.Threading;
using core;
using Cysharp.Threading.Tasks;
using UISystem;

namespace Game.Bootstrap.Loading.Operations
{
    public sealed class UiManagerConfigureOperation : LoadingOperationBase
    {
        private readonly UIManager _uiManager;
        private readonly WindowFactoryDI _windowFactoryDI;

        public UiManagerConfigureOperation(UIManager uiManager, WindowFactoryDI windowFactoryDI)
            : base(
                id: "ui_manager_configure",
                description: "Preparing UI systems",
                isCritical: true,
                weight: 0.1f,
                displayPriority: 110,
                retryPolicy: new LoadingRetryPolicy(1, TimeSpan.Zero),
                timeout: TimeSpan.FromSeconds(5))
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _windowFactoryDI = windowFactoryDI ?? throw new ArgumentNullException(nameof(windowFactoryDI));
        }

        protected override UniTask ExecuteInternalAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var eventHandler = new UIManagerSignalHandler();
            _uiManager.Configurate(_windowFactoryDI, eventHandler);
            ReportProgress(1f);
            return UniTask.CompletedTask;
        }
    }
}
