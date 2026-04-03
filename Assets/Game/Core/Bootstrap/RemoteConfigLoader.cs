using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using UnityEngine;

namespace Game.Bootstrap
{
    public class RemoteConfigLoader
    {
        public bool IsReady { get; private set; }

        public async UniTask InitAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();
            if (dependencyStatus != DependencyStatus.Available)
            {
                throw new InvalidOperationException($"Firebase dependencies not available: {dependencyStatus}");
            }

            var settings = new ConfigSettings
            {
                MinimumFetchIntervalInMilliseconds = 0
            };

            await FirebaseRemoteConfig.DefaultInstance.SetConfigSettingsAsync(settings).AsUniTask();
            ct.ThrowIfCancellationRequested();

            await FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync().AsUniTask();
            ct.ThrowIfCancellationRequested();

            Debug.Log("[RemoteConfigLoader] Remote Config fetched and activated.");
        }
        
        public void Init()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
                if (task.Result == DependencyStatus.Available) {
                    StartLoadingConfig();
                } else {
                    Debug.LogError($"Could not resolve all Firebase dependencies: {task.Result}");
                }
            });
        }

        private void StartLoadingConfig()
        {
            var settings = new ConfigSettings { MinimumFetchIntervalInMilliseconds = 0 };
            FirebaseRemoteConfig.DefaultInstance.SetConfigSettingsAsync(settings);
            FetchRemoteData();
        }

        private void FetchRemoteData()
        {
            FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync().ContinueWithOnMainThread(task => {
                if (task.IsCompletedSuccessfully) {
                    IsReady = true;
                    Debug.Log("Remote Config ready and activated!");
                } else {
                    Debug.LogError("Fetch failed. Using cached or default values.");
                }
            });
        }
    }
}