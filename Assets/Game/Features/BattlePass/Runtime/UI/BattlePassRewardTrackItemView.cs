using TMPro;
using UIShared;
using UnityEngine;

namespace BattlePass
{
    public sealed class BattlePassRewardTrackItemView : MonoBehaviour, ICleanup
    {
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private GameObject _emptyRewardsRoot;
        [SerializeField] private UIListPool<BattlePassRewardView> _rewardsPool;

        public void SetData(BattlePassTrackLevelUiModel model)
        {
            if (model == null)
            {
                Cleanup();
                return;
            }

            if (_levelText != null)
            {
                _levelText.text = model.Level.ToString();
            }

            if (_rewardsPool != null)
            {
                _rewardsPool.DisableAll();
                foreach (var reward in model.Rewards)
                {
                    var rewardView = _rewardsPool.GetNext();
                    rewardView.SetData(reward);
                }
            }

            if (_emptyRewardsRoot != null)
            {
                _emptyRewardsRoot.SetActive(model.Rewards == null || model.Rewards.Count == 0);
            }
        }

        public void Cleanup()
        {
            if (_levelText != null)
            {
                _levelText.text = string.Empty;
            }

            _rewardsPool?.DisableAll();

            if (_emptyRewardsRoot != null)
            {
                _emptyRewardsRoot.SetActive(true);
            }
        }
    }
}
