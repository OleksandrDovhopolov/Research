using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_LEVELPLAY
using Unity.Services.LevelPlay;
#endif

namespace Rewards
{
    [DisallowMultipleComponent]
    public sealed class IronSourceTestSuiteLauncherView : MonoBehaviour
    {
        [SerializeField] private Button _launchButton;
        [SerializeField] private bool _launchOnEnable;

        private void Awake()
        {
            if (_launchButton != null)
            {
                _launchButton.onClick.AddListener(HandleLaunchClicked);
            }
        }

        private void OnEnable()
        {
            if (_launchOnEnable)
            {
                LaunchTestSuite();
            }
        }

        private void OnDestroy()
        {
            if (_launchButton != null)
            {
                _launchButton.onClick.RemoveListener(HandleLaunchClicked);
            }
        }

        public void LaunchTestSuite()
        {
#if UNITY_LEVELPLAY
            if (TryLaunchIronSourceTestSuite(out var details))
            {
                Debug.Log($"[AdsDebug] IronSource Test Suite launched. {details}");
                return;
            }

            Debug.LogWarning($"[AdsDebug] Failed to launch IronSource Test Suite. {details}");
#else
            Debug.LogWarning("[AdsDebug] UNITY_LEVELPLAY is not enabled. Cannot launch IronSource Test Suite.");
#endif
        }

        private void HandleLaunchClicked()
        {
            LaunchTestSuite();
        }

#if UNITY_LEVELPLAY
        private static bool TryLaunchIronSourceTestSuite(out string details)
        {
            try
            {
                LevelPlay.LaunchTestSuite();
                details = "API='Unity.Services.LevelPlay.LevelPlay.LaunchTestSuite'.";
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[AdsDebug] LevelPlay.LaunchTestSuite direct call failed. {exception.Message}");
            }

            var typeCandidates = new[]
            {
                "IronSource.Agent",
                "IronSource",
                "Unity.Services.LevelPlay.LevelPlay"
            };

            var methodCandidates = new[]
            {
                "launchTestSuite",
                "LaunchTestSuite"
            };

            foreach (var typeName in typeCandidates)
            {
                var type = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(assembly => assembly.GetType(typeName, false))
                    .FirstOrDefault(candidate => candidate != null);

                if (type == null)
                {
                    continue;
                }

                foreach (var methodName in methodCandidates)
                {
                    var method = type.GetMethod(
                        methodName,
                        BindingFlags.Public | BindingFlags.Static,
                        binder: null,
                        types: Type.EmptyTypes,
                        modifiers: null);

                    if (method == null)
                    {
                        continue;
                    }

                    method.Invoke(null, null);
                    details = $"Type='{type.FullName}', Method='{method.Name}'.";
                    return true;
                }
            }

            details = "No compatible IronSource launchTestSuite API found in loaded assemblies.";
            return false;
        }
#endif
    }
}
