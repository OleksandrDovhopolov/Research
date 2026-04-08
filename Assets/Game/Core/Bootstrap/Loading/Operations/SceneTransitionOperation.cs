using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIShared.AnimationTransitionService;
using UnityEngine.SceneManagement;

namespace Game.Bootstrap.Loading
{
    public sealed class SceneTransitionOperation : LoadingOperationBase
    {
        private readonly string _sceneName;
        private readonly TransitionAnimationService _transitionAnimationService;

        public SceneTransitionOperation(string sceneName, TransitionAnimationService  transitionAnimationService)
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
            _transitionAnimationService = transitionAnimationService;
        }

        protected override async UniTask ExecuteInternalAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(_sceneName))
            {
                ReportProgress(1f);
                return;
            }
            
            await UniTask.Yield(PlayerLoopTiming.Update, ct);

            await _transitionAnimationService.PlayCoverAsync(ct);
            var asyncOperation = SceneManager.LoadSceneAsync(_sceneName);
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

            ReportProgress(1f);
        }
    }
}
