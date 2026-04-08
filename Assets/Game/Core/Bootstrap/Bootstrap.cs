using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Bootstrap.Loading;
using Infrastructure;
using UIShared;
using UIShared.Loading;
using UISystem;
using UnityEngine;
using VContainer;

namespace Game.Bootstrap
{
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private float _minimumLoadingSeconds = 2f;
        [SerializeField] private float _globalLoadingTimeoutSeconds = 60f;
        [SerializeField] private string _mainSceneName;
        [SerializeField] private LoadingScreenView _loadingScreenView;

        private CancellationToken _destroyCt;
        
        private UIManager _uiManager;
        private WindowFactoryDI _windowFactoryDI;
        private TransitionAnimationService _transitionAnimationService;
        private SaveService _saveService;
        private IObjectResolver _resolver;
        private RemoteConfigLoader _remoteConfigLoader;
        private LoadingOrchestrator _loadingOrchestrator;
        private IAuthorizationService _authorizationService;
        
        [Inject]
        private void Construct(
            UIManager uiManager,
            SaveService saveService,
            WindowFactoryDI  windowFactoryDI,
            RemoteConfigLoader remoteConfigLoader,
            IAuthorizationService authorizationService,
            LoadingOrchestrator loadingOrchestrator,
            TransitionAnimationService  transitionAnimationService)
        {
            _uiManager = uiManager;
            _windowFactoryDI = windowFactoryDI;
            _saveService = saveService;
            _remoteConfigLoader = remoteConfigLoader;
            _authorizationService = authorizationService;
            _loadingOrchestrator = loadingOrchestrator;
            _transitionAnimationService = transitionAnimationService;
        }
        
        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            Debug.LogWarning($"[Debug] Start {GetType().Name}");
            
            _destroyCt = this.GetCancellationTokenOnDestroy();
            
            RunBootstrapAsync(_destroyCt).Forget();
        }
        
        private async UniTask RunBootstrapAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_loadingScreenView == null)
            {
                Debug.LogError("[Bootstrap] LoadingScreenView is not assigned.");
                return;
            }

            _loadingScreenView.SetVisible(true);
            _loadingScreenView.SetErrorVisible(false);

            using var authGate = new LoadingAuthorizationGate(_authorizationService, _loadingScreenView);
            var phases = BuildPhases(authGate);
            _loadingOrchestrator.SetPhases(phases);
            _loadingOrchestrator.ProgressChanged += OnProgressChanged;
            _loadingOrchestrator.ActiveDescriptionChanged += OnActiveDescriptionChanged;

            var startRealtime = Time.realtimeSinceStartupAsDouble;
            var startPhaseIndex = 0;
            var globalTimeout = _globalLoadingTimeoutSeconds > 0f
                ? TimeSpan.FromSeconds(_globalLoadingTimeoutSeconds)
                : (TimeSpan?)null;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var result = await _loadingOrchestrator.RunAsync(startPhaseIndex, globalTimeout, ct);
                    if (result.IsSuccess)
                    {
                        break;
                    }

                    _loadingScreenView.SetError("Check your internet connection and try again.");
                    _loadingScreenView.SetErrorVisible(true);
                    await _loadingScreenView.WaitForRetryClickAsync(ct);
                    _loadingScreenView.SetErrorVisible(false);

                    startPhaseIndex = result.FailedPhaseIndex ?? 0;
                    _loadingOrchestrator.ResetFromPhase(startPhaseIndex);
                }

                var minSeconds = Mathf.Max(0f, _minimumLoadingSeconds);
                var elapsed = Time.realtimeSinceStartupAsDouble - startRealtime;
                var delaySeconds = minSeconds - elapsed;
                if (delaySeconds > 0d)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: ct);
                }
            }
            finally
            {
                _loadingOrchestrator.ProgressChanged -= OnProgressChanged;
                _loadingOrchestrator.ActiveDescriptionChanged -= OnActiveDescriptionChanged;
            }
        }

        private IReadOnlyList<LoadingPhase> BuildPhases(IAuthorizationGate authGate)
        {
            var phase1 = new LoadingPhase("phase_technical_init", new[]
            {
                new LoadingGroup("phase_technical_seq", LoadingGroupExecutionMode.Sequential, new ILoadingOperation[]
                {
                    new UiManagerConfigureOperation(_uiManager, _windowFactoryDI),
                    new FirebaseDependenciesOperation(_remoteConfigLoader),
                    new AddressablesUpdateOperation(),
                })
            });

            var phase2 = new LoadingPhase("phase_authorization", new[]
            {
                new LoadingGroup("phase_authorization_seq", LoadingGroupExecutionMode.Sequential, new ILoadingOperation[]
                {
                    new AuthorizationGateOperation(authGate)
                })
            });

            var phase3 = new LoadingPhase("phase_data_load", new[]
            {
                new LoadingGroup("phase_data_parallel", LoadingGroupExecutionMode.Parallel, new ILoadingOperation[]
                {
                    new RemoteConfigFetchOperation(_remoteConfigLoader),
                    new SaveDataLoadOperation(_saveService)
                })
            });

            var phase4 = new LoadingPhase("phase_finalization", new[]
            {
                new LoadingGroup("phase_finalization_seq", LoadingGroupExecutionMode.Sequential, new ILoadingOperation[]
                {
                    new WarmupOperation(),
                    new SceneTransitionOperation(_mainSceneName, _transitionAnimationService)
                })
            });

            return new[] { phase1, phase2, phase3, phase4 };
        }

        private void OnProgressChanged(float value)
        {
            _loadingScreenView?.SetProgress(value);
        }

        private void OnActiveDescriptionChanged(string description)
        {
            _loadingScreenView?.SetStatus(description);
        }
    }
}