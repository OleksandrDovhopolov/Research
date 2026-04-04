using System.Threading;
using CoreResources;
using Cysharp.Threading.Tasks;
using Game.Bootstrap.Loading;
using Game.Bootstrap.Loading.Operations;
using Infrastructure.SaveSystem;
using UIShared.Loading;
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
        
        private SaveService _saveService;
        private ResourceManager _resourceManager;
        private RemoteConfigLoader _remoteConfigLoader;
        private LoadingOrchestrator _loadingOrchestrator;
        private IAuthorizationService _authorizationService;
        
        [Inject]
        private void Construct(
            ResourceManager resourceManager,
            SaveService saveService,
            RemoteConfigLoader remoteConfigLoader,
            IAuthorizationService authorizationService,
            LoadingOrchestrator loadingOrchestrator)
        {
            _saveService = saveService;
            _resourceManager = resourceManager;
            _remoteConfigLoader = remoteConfigLoader;
            _authorizationService = authorizationService;
            _loadingOrchestrator = loadingOrchestrator;
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
                ? System.TimeSpan.FromSeconds(_globalLoadingTimeoutSeconds)
                : (System.TimeSpan?)null;

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
                    await UniTask.Delay(System.TimeSpan.FromSeconds(delaySeconds), cancellationToken: ct);
                }
            }
            finally
            {
                _loadingOrchestrator.ProgressChanged -= OnProgressChanged;
                _loadingOrchestrator.ActiveDescriptionChanged -= OnActiveDescriptionChanged;
            }
        }

        //TODO move to factory ? 
        private System.Collections.Generic.IReadOnlyList<LoadingPhase> BuildPhases(IAuthorizationGate authGate)
        {
            var phase1 = new LoadingPhase("phase_technical_init", new[]
            {
                new LoadingGroup("phase_technical_seq", LoadingGroupExecutionMode.Sequential, new ILoadingOperation[]
                {
                    //new UiManagerConfigureOperation(_uiManager, _resolver),
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
                    new SaveDataLoadOperation(_saveService),
                    new ResourceInitializationOperation(_resourceManager)
                })
            });

            var phase4 = new LoadingPhase("phase_finalization", new[]
            {
                new LoadingGroup("phase_finalization_seq", LoadingGroupExecutionMode.Sequential, new ILoadingOperation[]
                {
                    new WarmupOperation(),
                    new SceneTransitionOperation(_mainSceneName)
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

        private void OnDestroy()
        {
            //TODO move resources from loading phase
            //_resourceManager?.Dispose();
        }
    }
}