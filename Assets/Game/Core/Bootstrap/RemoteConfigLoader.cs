using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.RemoteConfig;
using UnityEngine;
using Unity.Services.Core;

namespace Game.Bootstrap
{
    public class RemoteConfigLoader
    {
        public bool IsReady { get; private set; }
        private bool _dependenciesReady;
        private bool _unityServicesReady;

        public async UniTask EnsureDependenciesAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await EnsureUnityServicesAsync(ct);

            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();
            if (dependencyStatus != DependencyStatus.Available)
            {
                throw new InvalidOperationException($"Firebase dependencies not available: {dependencyStatus}");
            }

            _dependenciesReady = true;
        }

        private async UniTask EnsureUnityServicesAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (_unityServicesReady)
            {
                return;
            }

            switch (UnityServices.State)
            {
                case ServicesInitializationState.Initialized:
                    _unityServicesReady = true;
                    return;

                case ServicesInitializationState.Initializing:
                    await UniTask.WaitUntil(
                        () => UnityServices.State == ServicesInitializationState.Initialized,
                        cancellationToken: ct);
                    _unityServicesReady = true;
                    return;
            }

            await UnityServices.InitializeAsync().AsUniTask();
            ct.ThrowIfCancellationRequested();
            _unityServicesReady = true;
            Debug.Log("[RemoteConfigLoader] Unity Gaming Services initialized.");
        }

        public async UniTask FetchAndActivateAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!_dependenciesReady)
            {
                await EnsureDependenciesAsync(ct);
            }

            var settings = new ConfigSettings
            {
                MinimumFetchIntervalInMilliseconds = 0
            };

            await FirebaseRemoteConfig.DefaultInstance.SetConfigSettingsAsync(settings).AsUniTask();
            ct.ThrowIfCancellationRequested();

            await FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync().AsUniTask();
            ct.ThrowIfCancellationRequested();

            IsReady = true;
            Debug.Log("[RemoteConfigLoader] Remote Config fetched and activated.");
        }
    }
}
