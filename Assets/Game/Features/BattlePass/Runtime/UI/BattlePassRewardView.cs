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
        [SerializeField] private GameObject _claimButtonRoot;
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _premiumPlaceholderCtaButton;

        private BattlePassRewardUiModel _currentReward;

        public event System.Action<int, BattlePassRewardTrack> ClaimClick;
        public event System.Action PremiumPlaceholderCtaClick;

        private void Awake()
        {
            if (_claimButton != null)
            {
                _claimButton.onClick.AddListener(HandleClaimClicked);
            }

            if (_premiumPlaceholderCtaButton != null)
            {
                _premiumPlaceholderCtaButton.onClick.AddListener(HandlePremiumPlaceholderCtaClicked);
            }
        }

        public void SetData(BattlePassRewardUiModel reward, Sprite premiumPlaceholderIcon = null)
        {
            if (reward == null)
            {
                Cleanup();
                return;
            }

            _currentReward = reward;
            var isPremiumPlaceholder = reward.IsPremiumPlaceholderLevelZero;

            if (_iconImage != null)
            {
                _iconImage.sprite = isPremiumPlaceholder && premiumPlaceholderIcon != null
                    ? premiumPlaceholderIcon
                    : reward.Icon;
            }

            if (_amountText != null)
            {
                _amountText.text = isPremiumPlaceholder ? string.Empty : reward.Amount.ToString();
            }

            if (_claimedStateRoot != null)
            {
                _claimedStateRoot.SetActive(reward.IsClaimed);
            }

            if (_lockedStateRoot != null)
            {
                _lockedStateRoot.SetActive(reward.IsPremiumTrack && reward.IsLocked && !reward.IsClaimed);
            }

            var canClaim = reward.IsClaimable && !isPremiumPlaceholder;

            if (_claimButtonRoot != null)
            {
                _claimButtonRoot.SetActive(canClaim);
            }

            if (_claimButton != null)
            {
                _claimButton.gameObject.SetActive(canClaim);
                _claimButton.interactable = canClaim;
            }

            if (_premiumPlaceholderCtaButton != null)
            {
                _premiumPlaceholderCtaButton.enabled = isPremiumPlaceholder;
                _premiumPlaceholderCtaButton.interactable = isPremiumPlaceholder;
            }
        }

        public void SetClaimInteractable(bool isInteractable)
        {
            if (_claimButton == null)
            {
                return;
            }

            var canClaim = _currentReward != null && _currentReward.IsClaimable;
            _claimButton.interactable = canClaim && isInteractable;
        }

        public void Cleanup()
        {
            _currentReward = null;

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

            if (_claimButtonRoot != null)
            {
                _claimButtonRoot.SetActive(false);
            }

            if (_claimButton != null)
            {
                _claimButton.gameObject.SetActive(false);
                _claimButton.interactable = false;
            }

            if (_premiumPlaceholderCtaButton != null)
            {
                _premiumPlaceholderCtaButton.enabled = false;
                _premiumPlaceholderCtaButton.interactable = false;
            }
        }

        private void HandleClaimClicked()
        {
            if (_currentReward == null)
            {
                return;
            }

            if (_currentReward.IsPremiumPlaceholderLevelZero)
            {
                PremiumPlaceholderCtaClick?.Invoke();
                return;
            }

            if (!_currentReward.IsClaimable)
            {
                return;
            }

            ClaimClick?.Invoke(_currentReward.Level, _currentReward.RewardTrack);
        }

        private void HandlePremiumPlaceholderCtaClicked()
        {
            if (_currentReward == null || !_currentReward.IsPremiumPlaceholderLevelZero)
            {
                return;
            }

            PremiumPlaceholderCtaClick?.Invoke();
        }

        private void OnDestroy()
        {
            if (_claimButton != null)
            {
                _claimButton.onClick.RemoveListener(HandleClaimClicked);
            }

            if (_premiumPlaceholderCtaButton != null)
            {
                _premiumPlaceholderCtaButton.onClick.RemoveListener(HandlePremiumPlaceholderCtaClicked);
            }
        }
    }
}
