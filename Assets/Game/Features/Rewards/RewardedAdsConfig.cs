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
        public string AndroidLevelPlayAppKey = string.Empty;
        public string IosLevelPlayAppKey = string.Empty;
        public string AndroidLevelPlayRewardedAdUnitId = string.Empty;
        public string IosLevelPlayRewardedAdUnitId = string.Empty;
        public string RewardId = "Gems";
        public bool TestMode = true;
        public int GrantTimeoutSeconds = 15;
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

        public string GetLevelPlayAppKeyForCurrentPlatform()
        {
#if UNITY_ANDROID
            return AndroidLevelPlayAppKey;
#elif UNITY_IOS
            return IosLevelPlayAppKey;
#else
            return !string.IsNullOrWhiteSpace(AndroidLevelPlayAppKey) ? AndroidLevelPlayAppKey : IosLevelPlayAppKey;
#endif
        }

        public string GetLevelPlayRewardedAdUnitIdForCurrentPlatform()
        {
#if UNITY_ANDROID
            return AndroidLevelPlayRewardedAdUnitId;
#elif UNITY_IOS
            return IosLevelPlayRewardedAdUnitId;
#else
            return !string.IsNullOrWhiteSpace(AndroidLevelPlayRewardedAdUnitId)
                ? AndroidLevelPlayRewardedAdUnitId
                : IosLevelPlayRewardedAdUnitId;
#endif
        }

        public int GetGrantTimeoutSecondsOrDefault(int fallback)
        {
            return GrantTimeoutSeconds > 0 ? GrantTimeoutSeconds : fallback;
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
