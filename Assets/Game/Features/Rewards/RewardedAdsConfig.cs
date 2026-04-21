using System;
using UnityEngine;

namespace Rewards
{
    [Serializable]
    public sealed class RewardedAdsConfig
    {
        public RewardedAdsMode Mode = RewardedAdsMode.Mock;
        public string AndroidGameId = string.Empty;
        public string IosGameId = string.Empty;
        public string AndroidRewardedAdUnitId = string.Empty;
        public string IosRewardedAdUnitId = string.Empty;
        public string RewardId = "Gems";
        public bool TestMode = true;
        public bool UseServerConfirmedGrantFlow;
        public int GrantTimeoutSeconds = 15;
        public int GrantConfirmationTimeoutSeconds = 20;
        public float GrantPollingIntervalSeconds = 1f;
        public bool EnableIntentPollingLogs = true;
        public MockAdsOutcome MockOutcome = MockAdsOutcome.Success;
        public bool UseRandomMockDelay = true;
        public Vector2 MockDelayRangeSeconds = new(1f, 2f);
        public float MockDelaySeconds = 1.5f;

        public string GetGameIdForCurrentPlatform()
        {
#if UNITY_ANDROID
            return AndroidGameId;
#elif UNITY_IOS
            return IosGameId;
#else
            return !string.IsNullOrWhiteSpace(AndroidGameId) ? AndroidGameId : IosGameId;
#endif
        }

        public string GetRewardedAdUnitIdForCurrentPlatform()
        {
#if UNITY_ANDROID
            return AndroidRewardedAdUnitId;
#elif UNITY_IOS
            return IosRewardedAdUnitId;
#else
            return !string.IsNullOrWhiteSpace(AndroidRewardedAdUnitId)
                ? AndroidRewardedAdUnitId
                : IosRewardedAdUnitId;
#endif
        }

        public int GetGrantTimeoutSecondsOrDefault(int fallback)
        {
            return GrantTimeoutSeconds > 0 ? GrantTimeoutSeconds : fallback;
        }

        public int GetGrantConfirmationTimeoutSecondsOrDefault(int fallback)
        {
            return GrantConfirmationTimeoutSeconds > 0 ? GrantConfirmationTimeoutSeconds : fallback;
        }

        public float GetGrantPollingIntervalSecondsOrDefault(float fallback)
        {
            var interval = GrantPollingIntervalSeconds > 0f ? GrantPollingIntervalSeconds : fallback;
            return Mathf.Max(0.1f, interval);
        }

        public float GetMockDelaySeconds()
        {
            if (!UseRandomMockDelay)
            {
                return Mathf.Max(0.05f, MockDelaySeconds);
            }

            var min = Mathf.Min(MockDelayRangeSeconds.x, MockDelayRangeSeconds.y);
            var max = Mathf.Max(MockDelayRangeSeconds.x, MockDelayRangeSeconds.y);
            min = Mathf.Max(0.05f, min);
            max = Mathf.Max(min, max);
            return UnityEngine.Random.Range(min, max);
        }
    }
}
