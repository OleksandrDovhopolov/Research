using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.RemoteConfig;
using UnityEngine;

namespace Game.Bootstrap
{
    public class RemoteConfigLoader
    {
        public bool IsReady { get; private set; }
        private bool _dependenciesReady;

        public async UniTask EnsureDependenciesAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();
            if (dependencyStatus != DependencyStatus.Available)
            {
                throw new InvalidOperationException($"Firebase dependencies not available: {dependencyStatus}");
            }

            _dependenciesReady = true;
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