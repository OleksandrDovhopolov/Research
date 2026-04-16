using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        private IFortuneWheelServerService _fortuneWheelServerService;
        
        [Inject]
        public void Install(UIManager uiManager, IFortuneWheelServerService  fortuneWheelServerService)
        {
            _uiManager = uiManager;
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

                var sectorData = new FortuneWheelSectorArgs(reward.RewardId, _defaultSprite, 1);
                sectors.Add(sectorData);
            }

            if (sectors.Count != SectorCount)
            {
                Debug.LogWarning($"[CheatFortuneWheelButton] Failed to build {SectorCount} sectors. Built: {sectors.Count}.");
                return;
            }

            var args = new FortuneWheelArgs(data.AvailableSpins, timeSpan, sectors);

            Debug.LogWarning($"[Debug] CheatFortuneWheelButton {data.AvailableSpins} / {timeSpan} / {rewards.Count} - {sectors.Count}");
            _uiManager.Show<FortuneWheelController>(args);
        }
    }
}
