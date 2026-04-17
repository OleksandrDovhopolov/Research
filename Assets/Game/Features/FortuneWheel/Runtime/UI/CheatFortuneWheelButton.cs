using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Rewards;
using UISystem;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace FortuneWheel
{
    public class CheatFortuneWheelButton : MonoBehaviour
    {
        private const int SectorCount = 8;

        [SerializeField] private Button _cheatButton;
        [SerializeField] private Sprite _defaultSprite;

        private CancellationToken _destroyCt;
        
        private UIManager _uiManager;
        private IRewardSpecProvider _rewardSpecProvider;
        private IFortuneWheelServerService _fortuneWheelServerService;
        
        [Inject]
        public void Install(UIManager uiManager, IRewardSpecProvider rewardSpecProvider, IFortuneWheelServerService  fortuneWheelServerService)
        {
            _uiManager = uiManager;
            _rewardSpecProvider = rewardSpecProvider;
            _fortuneWheelServerService = fortuneWheelServerService;
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
            
            var data = await _fortuneWheelServerService.GetDataSync(ct);
            var timeSpan = TimeSpan.FromSeconds(Mathf.Max(0, data.NextRegenSeconds));

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

            Debug.LogWarning($"[Debug] CheatFortuneWheelButton {data.AvailableSpins} / {timeSpan} / {rewards.Count} - {sectors.Count}");
            var args = new FortuneWheelArgs(data.AvailableSpins, timeSpan, sectors);
            _uiManager.Show<FortuneWheelController>(args);
        }
    }
}
