using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BattlePass
{
    public sealed class BattlePassServerService : IBattlePassServerService
    {
        private readonly IWebClient _webClient;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;

        public BattlePassServerService(IWebClient webClient, IPlayerIdentityProvider playerIdentityProvider)
        {
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
        }

        public async UniTask<BattlePassSnapshot> GetCurrentAsync(CancellationToken ct = default)
        {
            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new InvalidOperationException("Player id is empty.");
            }

            var requestUrl = $"{BattlePassConfig.Api.CurrentPath}?playerId={Uri.EscapeDataString(playerId)}";
            var response = await _webClient.GetAsync<BattlePassCurrentResponse>(requestUrl, ct);

            return MapResponse(response);
        }

        private static BattlePassSnapshot MapResponse(BattlePassCurrentResponse response)
        {
            var season = MapSeason(response?.Season);
            var products = MapProducts(response?.Products);
            var userState = MapUserState(response?.UserState);
            var levels = response?.Levels?
                .Where(level => level != null)
                .Select(MapLevel)
                .Where(level => level != null)
                .OrderBy(level => level.Level)
                .ToArray() ?? Array.Empty<BattlePassLevel>();

            var fallbackServerTime = season?.EndAtUtc ?? DateTimeOffset.MinValue;
            var serverTimeUtc = ParseUtcOrFallback(response?.ServerTimeUtc, fallbackServerTime);

            return new BattlePassSnapshot(season, products, userState, levels, serverTimeUtc);
        }

        private static BattlePassSeason MapSeason(BattlePassSeasonResponse response)
        {
            if (response == null)
            {
                return null;
            }

            return new BattlePassSeason(
                response.Id,
                response.Title,
                ParseUtcOrFallback(response.StartAtUtc, DateTimeOffset.MinValue),
                ParseUtcOrFallback(response.EndAtUtc, DateTimeOffset.MinValue),
                response.MaxLevel,
                response.Status,
                response.ConfigVersion);
        }

        private static BattlePassProducts MapProducts(BattlePassProductsResponse response)
        {
            if (response == null)
            {
                return null;
            }

            var premiumProductId = !string.IsNullOrWhiteSpace(response.PremiumProductId)
                ? response.PremiumProductId
                : response.GoldProductId;

            return new BattlePassProducts(premiumProductId, response.PlatinumProductId);
        }

        private static BattlePassUserState MapUserState(BattlePassUserStateResponse response)
        {
            if (response == null)
            {
                return null;
            }

            return new BattlePassUserState(
                response.SeasonId,
                response.Level,
                response.Xp,
                MapPassType(response.PassType));
        }

        private static BattlePassLevel MapLevel(BattlePassLevelResponse response)
        {
            if (response == null)
            {
                return null;
            }

            return new BattlePassLevel(
                response.Level,
                response.XpRequired,
                ParseRewardRefs(response.DefaultRewards),
                ParseRewardRefs(response.PremiumRewards));
        }

        private static IReadOnlyList<BattlePassRewardRef> ParseRewardRefs(JToken rewardsToken)
        {
            if (rewardsToken is not JArray rewardsArray)
            {
                return Array.Empty<BattlePassRewardRef>();
            }

            var rewards = new List<BattlePassRewardRef>(rewardsArray.Count);
            foreach (var rewardToken in rewardsArray)
            {
                var rewardId = rewardToken.Type switch
                {
                    JTokenType.String => rewardToken.Value<string>(),
                    JTokenType.Object => rewardToken["rewardId"]?.Value<string>() ?? rewardToken["id"]?.Value<string>(),
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(rewardId))
                {
                    continue;
                }

                rewards.Add(new BattlePassRewardRef(rewardId));
            }

            return rewards;
        }

        private static BattlePassPassType MapPassType(string passType)
        {
            if (string.IsNullOrWhiteSpace(passType))
            {
                return BattlePassPassType.Unknown;
            }

            return passType.Trim().ToLowerInvariant() switch
            {
                "none" => BattlePassPassType.None,
                "gold" => BattlePassPassType.Premium,
                "premium" => BattlePassPassType.Premium,
                "platinum" => BattlePassPassType.Platinum,
                _ => BattlePassPassType.Unknown
            };
        }

        private static DateTimeOffset ParseUtcOrFallback(string rawValue, DateTimeOffset fallback)
        {
            return DateTimeOffset.TryParse(
                rawValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed)
                ? parsed
                : fallback;
        }

        [Serializable]
        private sealed class BattlePassCurrentResponse
        {
            [JsonProperty("season")]
            public BattlePassSeasonResponse Season { get; set; }

            [JsonProperty("products")]
            public BattlePassProductsResponse Products { get; set; }

            [JsonProperty("userState")]
            public BattlePassUserStateResponse UserState { get; set; }

            [JsonProperty("levels")]
            public BattlePassLevelResponse[] Levels { get; set; }

            [JsonProperty("serverTimeUtc")]
            public string ServerTimeUtc { get; set; }
        }

        [Serializable]
        private sealed class BattlePassSeasonResponse
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("startAtUtc")]
            public string StartAtUtc { get; set; }

            [JsonProperty("endAtUtc")]
            public string EndAtUtc { get; set; }

            [JsonProperty("maxLevel")]
            public int MaxLevel { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("configVersion")]
            public string ConfigVersion { get; set; }
        }

        [Serializable]
        private sealed class BattlePassProductsResponse
        {
            [JsonProperty("goldProductId")]
            public string GoldProductId { get; set; }

            [JsonProperty("premiumProductId")]
            public string PremiumProductId { get; set; }

            [JsonProperty("platinumProductId")]
            public string PlatinumProductId { get; set; }
        }

        [Serializable]
        private sealed class BattlePassUserStateResponse
        {
            [JsonProperty("seasonId")]
            public string SeasonId { get; set; }

            [JsonProperty("level")]
            public int Level { get; set; }

            [JsonProperty("xp")]
            public int Xp { get; set; }

            [JsonProperty("passType")]
            public string PassType { get; set; }
        }

        [Serializable]
        private sealed class BattlePassLevelResponse
        {
            [JsonProperty("level")]
            public int Level { get; set; }

            [JsonProperty("xpRequired")]
            public int XpRequired { get; set; }

            [JsonProperty("defaultRewards")]
            public JToken DefaultRewards { get; set; }

            [JsonProperty("premiumRewards")]
            public JToken PremiumRewards { get; set; }
        }
    }
}
