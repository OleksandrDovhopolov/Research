using System;
using System.Collections.Generic;
using cheatModule;
using Rewards;
using UISystem;
using UnityEngine;

namespace Game.Cheat
{
    public class RewardCheatModule : ICheatsModule
    {
        private const string RewardsGroup = "Rewards";

        private readonly UIManager _uiManager;
        private readonly RewardSpecsConfigSO _rewardSpecsConfigSo;

        public RewardCheatModule(UIManager uiManager, RewardSpecsConfigSO rewardSpecsConfigSo)
        {
            _uiManager = uiManager;
            _rewardSpecsConfigSo = rewardSpecsConfigSo;
        }

        public void Initialize(ICheatsContainer cheatsContainer)
        {
            if (_rewardSpecsConfigSo?.RewardSpecs == null || _rewardSpecsConfigSo.RewardSpecs.Count == 0)
            {
                Debug.LogWarning($"[{nameof(RewardCheatModule)}] Reward specs config is empty.");
                return;
            }

            var addedRewardIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rewardSpec in _rewardSpecsConfigSo.RewardSpecs)
            {
                var rewardId = rewardSpec?.RewardId;
                if (string.IsNullOrWhiteSpace(rewardId) || !addedRewardIds.Add(rewardId))
                {
                    continue;
                }

                cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick($"Open reward {rewardId}", () =>
                {
                    var rewardArgs = new RewardsWindowArgs(rewardId);
                    _uiManager.Show<RewardsWindowController>(rewardArgs);
                }).WithGroup(RewardsGroup));
            }
        }
    }
}
