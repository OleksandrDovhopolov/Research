using System.Collections.Generic;
using UnityEngine;

namespace Rewards
{
    [CreateAssetMenu(
        fileName = "RewardSpecsConfig",
        menuName = "Game/Rewards/Reward Specs Config",
        order = 0)]
    public sealed class RewardSpecsConfigSO : ScriptableObject
    {
        public List<RewardSpec> RewardSpecs = new();
    }
}
