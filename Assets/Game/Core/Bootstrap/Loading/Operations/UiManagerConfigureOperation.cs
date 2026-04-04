using System;
using System.Threading;
using core;
using Cysharp.Threading.Tasks;
using UISystem;
using VContainer;

namespace Game.Bootstrap.Loading.Operations
{
    public sealed class UiManagerConfigureOperation : LoadingOperationBase
    {
        private readonly UIManager _uiManager;
        private readonly IObjectResolver _resolver;

        public UiManagerConfigureOperation(UIManager uiManager, IObjectResolver resolver)
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
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        protected override UniTask ExecuteInternalAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var windowFactoryBase = new WindowFactoryDI(_uiManager, _resolver);
            var eventHandler = new UIManagerSignalHandler();
            _uiManager.Configurate(windowFactoryBase, eventHandler);
            ReportProgress(1f);
            return UniTask.CompletedTask;
        }
    }
}
