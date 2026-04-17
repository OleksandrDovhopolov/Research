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
            _cheatButton.onClick.AddListener(() => OpenCheatsPanelAsync(_destroyCt).Forget());
        }

        private async UniTask OpenCheatsPanelAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Yield();

            var playerId = _playerIdentityProvider?.GetPlayerId();
            if (!string.IsNullOrWhiteSpace(playerId))
            {
                var verificationUrl = BuildWheelDataUrl(playerId);
                Debug.Log(
                    $"{LogPrefix} Player context. PlayerId={playerId}, MaskedPlayerId={MaskPlayerId(playerId)}, " +
                    $"VerifyWith={verificationUrl}");
            }
            
            var data = await _fortuneWheelServerService.GetDataSync(ct);

            IReadOnlyList<FortuneWheelRewardServerItem> rewards = await _fortuneWheelServerService.GetRewardsAsync(ct);
            if (rewards == null || rewards.Count == 0)
            {
                Debug.LogWarning("[CheatFortuneWheelButton] Rewards list is empty.");
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
                    var rewardConfig = spec.Resources.First();
                    var sectorData = new FortuneWheelSectorArgs(reward.RewardId,rewardConfig.Icon, rewardConfig.Amount);
                    sectors.Add(sectorData);
                }
                else
                {
                    Debug.LogError($"[CheatFortuneWheelButton] Failed to find reward spec for {reward.RewardId}");
                }
            }

            if (sectors.Count != SectorCount)
            {
                Debug.LogWarning($"[CheatFortuneWheelButton] Failed to build {SectorCount} sectors. Built: {sectors.Count}.");
                return;
            }

            Debug.LogWarning($"[Debug] CheatFortuneWheelButton {data.AvailableSpins} / nextAt={data.NextUpdateAt} / {rewards.Count} - {sectors.Count}");
            var args = new FortuneWheelArgs(data, sectors);
            _uiManager.Show<FortuneWheelController>(args);
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
