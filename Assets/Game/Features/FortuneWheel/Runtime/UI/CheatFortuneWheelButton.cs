using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Rewards;
using UISystem;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VContainer;

namespace FortuneWheel
{
    public class CheatFortuneWheelButton : MonoBehaviour
    {
        private const string LogPrefix = "[CheatFortuneWheelButton]";
        private const int SectorCount = 8;

        [SerializeField] private Button _cheatButton;
        [SerializeField] private Sprite _defaultSprite;

        private CancellationToken _destroyCt;
        
        private UIManager _uiManager;
        private IRewardSpecProvider _rewardSpecProvider;
        private IFortuneWheelServerService _fortuneWheelServerService;
        private IPlayerIdentityProvider _playerIdentityProvider;
        
        [Inject]
        public void Install(
            UIManager uiManager,
            IRewardSpecProvider rewardSpecProvider,
            IFortuneWheelServerService fortuneWheelServerService,
            IPlayerIdentityProvider playerIdentityProvider)
        {
            _uiManager = uiManager;
            _rewardSpecProvider = rewardSpecProvider;
            _fortuneWheelServerService = fortuneWheelServerService;
            _playerIdentityProvider = playerIdentityProvider;
        }
        
        private void Awake()
        {
            _destroyCt = this.GetCancellationTokenOnDestroy();
        }

        private void Start()
        {
            if (_cheatButton == null)
            {
                Debug.LogError($"{LogPrefix} Cheat button is not assigned.");
                return;
            }

            _cheatButton.onClick.AddListener(() => OpenCheatsPanelAsync(_destroyCt).Forget());
        }

        private async UniTask OpenCheatsPanelAsync(CancellationToken ct)
        {
            IReadOnlyList<FortuneWheelRewardServerItem> rewards = await _fortuneWheelServerService.GetRewardsAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield();

                if (_uiManager == null || _rewardSpecProvider == null || _fortuneWheelServerService == null)
                {
                    Debug.LogError(
                        $"{LogPrefix} Dependencies are not installed. " +
                        $"HasUIManager={_uiManager != null}, HasRewardSpecProvider={_rewardSpecProvider != null}, " +
                        $"HasFortuneWheelServerService={_fortuneWheelServerService != null}");
                    return;
                }

                var playerId = _playerIdentityProvider?.GetPlayerId();
                if (!string.IsNullOrWhiteSpace(playerId))
                {
                    var verificationUrl = BuildWheelDataUrl(playerId);
                    Debug.Log(
                        $"{LogPrefix} Player context. PlayerId={playerId}, MaskedPlayerId={MaskPlayerId(playerId)}, " +
                        $"VerifyWith={verificationUrl}");
                }

                var data = await _fortuneWheelServerService.GetDataSync(ct);

                if (rewards == null || rewards.Count == 0)
                {
                    Debug.LogWarning($"{LogPrefix} Rewards list is empty.");
                    return;
                }

                var sectors = new List<FortuneWheelSectorArgs>(SectorCount);
                for (var i = 0; i < SectorCount; i++)
                {
                    var reward = rewards[i % rewards.Count];
                    if (reward == null || string.IsNullOrWhiteSpace(reward.RewardId))
                    {
                        continue;
                    }

                    if (_rewardSpecProvider.TryGet(reward.RewardId, out var spec))
                    {
                        var rewardConfig = spec?.Resources?.FirstOrDefault(resource =>
                            resource != null &&
                            !string.IsNullOrWhiteSpace(resource.ResourceId) &&
                            resource.Amount > 0);
                        if (rewardConfig == null)
                        {
                            Debug.LogError($"{LogPrefix} Reward spec has no valid resources. RewardId={reward.RewardId}");
                            continue;
                        }

                        var sectorData = new FortuneWheelSectorArgs(reward.RewardId, rewardConfig.Icon, rewardConfig.Amount);
                        sectors.Add(sectorData);
                    }
                    else
                    {
                        Debug.LogError($"{LogPrefix} Failed to find reward spec for {reward.RewardId}");
                    }
                }

                if (sectors.Count != SectorCount)
                {
                    Debug.LogWarning($"{LogPrefix} Failed to build {SectorCount} sectors. Built: {sectors.Count}.");
                    return;
                }

                Debug.LogWarning($"{LogPrefix} Open wheel. Spins={data.AvailableSpins}, nextAt={data.NextUpdateAt}, rewards={rewards.Count}, sectors={sectors.Count}");
                var args = new FortuneWheelArgs(data, sectors);
                _uiManager.Show<FortuneWheelController>(args);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} Failed to open wheel: {exception}");
            }
        }

        private static string BuildWheelDataUrl(string playerId)
        {
            var encodedPlayerId = UnityWebRequest.EscapeURL(playerId ?? string.Empty);
            return $"{ApiConfig.BaseUrl}wheel/data?playerId={encodedPlayerId}";
        }

        private static string MaskPlayerId(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return "<empty>";
            }

            if (playerId.Length <= 8)
            {
                return playerId;
            }

            return $"{playerId.Substring(0, 4)}...{playerId.Substring(playerId.Length - 4, 4)}";
        }
    }
}
