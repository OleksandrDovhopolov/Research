using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public class CollectedAmountProgressView : MonoBehaviour
    {
        [SerializeField] private Image _collectedSlider;
        [SerializeField] private TextMeshProUGUI _groupCollectedAmountText;
        [SerializeField] private float _animationDuration = 0.35f;

        private Tween _sliderTween;
        private bool _hasCollectedProgressValue;
        private float _lastCollectedProgressValue;
        private bool _hasLastAmounts;
        private int _lastCollectedAmount;
        private int _lastTotalAmount;

        public void SetPreviousProgress(int collectedAmount, int totalAmount)
        {
            _sliderTween?.Kill();
            _sliderTween = null;

            _groupCollectedAmountText.text = collectedAmount + " / " + totalAmount;

            _hasCollectedProgressValue = true;
            _lastCollectedProgressValue = GetTargetProgress(collectedAmount, totalAmount);
            _collectedSlider.fillAmount = _lastCollectedProgressValue;

            _hasLastAmounts = true;
            _lastCollectedAmount = collectedAmount;
            _lastTotalAmount = totalAmount;
        }

        public void UpdateCollectedAmountAnimated(int collectedAmount, int totalAmount, Action<bool> onAnimationCompleted = null)
        {
            if (_hasLastAmounts &&
                _lastCollectedAmount == collectedAmount &&
                _lastTotalAmount == totalAmount)
            {
                return;
            }

            _groupCollectedAmountText.text = collectedAmount + " / " + totalAmount;
            
            var targetProgress = GetTargetProgress(collectedAmount, totalAmount);

            _sliderTween?.Kill();

            if (_hasCollectedProgressValue == false)
            {
                _collectedSlider.fillAmount = targetProgress;
                _lastCollectedProgressValue = targetProgress;
                _hasCollectedProgressValue = true;
                _hasLastAmounts = true;
                _lastCollectedAmount = collectedAmount;
                _lastTotalAmount = totalAmount;
                return;
            }

            _collectedSlider.fillAmount = _lastCollectedProgressValue;
            _sliderTween = _collectedSlider
                .DOFillAmount(targetProgress, _animationDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    onAnimationCompleted?.Invoke(collectedAmount == totalAmount);
                    _sliderTween = null;
                });

            _lastCollectedProgressValue = targetProgress;
            _hasLastAmounts = true;
            _lastCollectedAmount = collectedAmount;
            _lastTotalAmount = totalAmount;
        }

        private float GetTargetProgress(int collectedAmount, int totalAmount)
        {
            var targetProgress = totalAmount > 0
                ? Mathf.Clamp01((float)collectedAmount / totalAmount)
                : 0f;
            return targetProgress;
        }
        
        private void OnDestroy()
        {
            _sliderTween?.Kill();
        }
    }
}
