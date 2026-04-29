using TMPro;
using UIShared;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass
{
    public class BattlePassSliderLevelView : MonoBehaviour, ICleanup
    {
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Image _levelZeroIcon;

        private RectTransform _rectTransform;

        public RectTransform RectTransform => _rectTransform != null
            ? _rectTransform
            : _rectTransform = transform as RectTransform;

        public virtual void SetLevel(int level, Sprite zeroLevelSprite)
        {
            var safeLevel = Mathf.Max(0, level);
            var isLevelZero = safeLevel == 0;

            if (_levelText != null)
            {
                _levelText.gameObject.SetActive(!isLevelZero);
                _levelText.text = isLevelZero ? string.Empty : safeLevel.ToString();
            }

            if (_levelZeroIcon != null)
            {
                _levelZeroIcon.gameObject.SetActive(isLevelZero);
                _levelZeroIcon.sprite = isLevelZero ? zeroLevelSprite : null;
            }
        }

        public virtual void SetAnchoredX(float anchoredX)
        {
            if (RectTransform == null)
            {
                return;
            }

            var anchoredPosition = RectTransform.anchoredPosition;
            anchoredPosition.x = anchoredX;
            RectTransform.anchoredPosition = anchoredPosition;
        }

        public virtual void Cleanup()
        {
            if (_levelText != null)
            {
                _levelText.text = string.Empty;
                _levelText.gameObject.SetActive(false);
            }

            if (_levelZeroIcon != null)
            {
                _levelZeroIcon.sprite = null;
                _levelZeroIcon.gameObject.SetActive(false);
            }

            if (RectTransform != null)
            {
                var anchoredPosition = RectTransform.anchoredPosition;
                anchoredPosition.x = 0f;
                RectTransform.anchoredPosition = anchoredPosition;
            }
        }
    }
}
