using TMPro;
using UIShared;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass
{
    public sealed class BattlePassRewardView : MonoBehaviour, ICleanup
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private GameObject _claimedStateRoot;
        [SerializeField] private GameObject _lockedStateRoot;

        public void SetData(BattlePassRewardUiModel reward)
        {
            if (reward == null)
            {
                Cleanup();
                return;
            }

            if (_iconImage != null)
            {
                _iconImage.sprite = reward.Icon;
            }

            if (_amountText != null)
            {
                _amountText.text = reward.Amount.ToString();
            }

            if (_claimedStateRoot != null)
            {
                _claimedStateRoot.SetActive(reward.IsClaimed);
            }

            if (_lockedStateRoot != null)
            {
                _lockedStateRoot.SetActive(reward.IsPremiumTrack && reward.IsLocked && !reward.IsClaimed);
            }
        }

        public void Cleanup()
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = null;
            }

            if (_amountText != null)
            {
                _amountText.text = string.Empty;
            }

            if (_claimedStateRoot != null)
            {
                _claimedStateRoot.SetActive(false);
            }

            if (_lockedStateRoot != null)
            {
                _lockedStateRoot.SetActive(false);
            }
        }
    }
}
