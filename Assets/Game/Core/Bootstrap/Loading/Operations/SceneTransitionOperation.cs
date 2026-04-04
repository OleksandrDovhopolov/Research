using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Bootstrap.Loading.Operations
{
    public sealed class SceneTransitionOperation : LoadingOperationBase
    {
        private readonly string _sceneName;

        public SceneTransitionOperation(string sceneName)
            : base(
                id: "scene_transition",
                description: "Entering main game",
                isCritical: true,
                weight: 0.15f,
                displayPriority: 60,
                retryPolicy: new LoadingRetryPolicy(1, TimeSpan.Zero),
                timeout: TimeSpan.FromSeconds(15))
        {
            _sceneName = sceneName;
        }

        protected override async UniTask ExecuteInternalAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(_sceneName))
            {
                ReportProgress(1f);
                return;
            }
            
            await UniTask.DelayFrame(100, cancellationToken: ct);
            Debug.LogWarning($"[Debug] {GetType().Name} loading scene '{_sceneName}'");
            ReportProgress(1f);
            
            /*var asyncOperation = SceneManager.LoadSceneAsync(_sceneName);
            if (asyncOperation == null)
            {
                throw new InvalidOperationException($"Failed to load scene '{_sceneName}'.");
            }

            while (!asyncOperation.isDone)
            {
                ct.ThrowIfCancellationRequested();
                ReportProgress(asyncOperation.progress >= 0.9f ? 1f : asyncOperation.progress);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            ReportProgress(1f);*/
        }
    }
}
