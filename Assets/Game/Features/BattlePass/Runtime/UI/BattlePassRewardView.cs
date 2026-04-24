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
        }
    }
}
