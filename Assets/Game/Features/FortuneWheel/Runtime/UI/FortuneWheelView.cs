using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace FortuneWheel
{
    public class FortuneWheelView : WindowView
    {
        [Serializable]
        private struct SectorView
        {
            [SerializeField] private Image _rewardImage;
            [SerializeField] private TextMeshProUGUI _rewardAmountText;

            public void SetData(FortuneWheelSectorArgs sector)
            {
                if (_rewardImage != null)
                {
                    _rewardImage.sprite = sector.RewardIcon;
                }

                if (_rewardAmountText != null)
                {
                    _rewardAmountText.text = sector.RewardAmount.ToString();
                }
            }

            public void Clear()
            {
                if (_rewardImage != null)
                {
                    _rewardImage.sprite = null;
                }

                if (_rewardAmountText != null)
                {
                    _rewardAmountText.text = string.Empty;
                }
            }
        }

        // In current prefab setup, sector index 0 is already aligned with 12 o'clock at Z = 0.
        private const float PointerAngle = 0f;
        private const float FullCircle = 360f;
        private const float MinSpinDuration = 0.01f;
        private const float SectorAngle = FullCircle / FortuneWheelArgs.SectorCount;

        [Header("Top")]
        [SerializeField] private TMP_Text _spinsAmountText;
        [SerializeField] private TMP_Text _timerText;

        [Header("Spin")]
        [SerializeField] private Button _spinButton;
        [SerializeField] private Button _spinAdButton;
        [SerializeField] private RectTransform _wheelRoot;
        [SerializeField] private float _spinDuration = 4f;
        [SerializeField] private int _spinFullTurns = 4;

        [Header("Close Lock")]
        [SerializeField] private GameObject _clickLocker;

        [Header("Sectors")]
        [SerializeField] private SectorView[] _sectors = new SectorView[FortuneWheelArgs.SectorCount];

        private Tween _spinTween;

        public event Action SpinClick;
        public event Action SpinAdClick;

        protected override void Awake()
        {
            base.Awake();

            _spinButton.onClick.AddListener(HandleSpinClick);
            _spinAdButton.onClick.AddListener(HandleSpinAdClick);
        }

        public void SetData(FortuneWheelArgs args)
        {
            if (args == null)
            {
                Debug.LogError($"[{nameof(FortuneWheelView)}] {nameof(FortuneWheelArgs)} are missing.");
                return;
            }

            SetSectors(args.Sectors);
        }

        public void SetSpinsAmount(int spinsAmount)
        {
            if (_spinsAmountText != null)
            {
                _spinsAmountText.text = spinsAmount.ToString();
            }
        }

        public void SetRemainingTime(TimeSpan remainingTime)
        {
            if (remainingTime < TimeSpan.Zero)
            {
                remainingTime = TimeSpan.Zero;
            }

            UpdateTimerLabel(remainingTime);
        }

        public void SetSpinInteractable(bool isInteractable)
        {
            SetSpinButtonInteractable(isInteractable);
            SetSpinAdButtonInteractable(isInteractable);
        }

        public void SetSpinButtonInteractable(bool isInteractable)
        {
            if (_spinButton != null)
            {
                _spinButton.interactable = isInteractable;
            }
        }

        public void SetSpinAdButtonInteractable(bool isInteractable)
        {
            if (_spinAdButton != null)
            {
                _spinAdButton.interactable = isInteractable;
            }
        }

        public void SetCloseInteractable(bool isInteractable)
        {
            _clickLocker.gameObject.SetActive(isInteractable);
        }

        public bool PlaySpinToSector(int sectorIndex, Action onComplete)
        {
            if (_wheelRoot == null)
            {
                Debug.LogError($"[{nameof(FortuneWheelView)}] Wheel root is not assigned.");
                return false;
            }

            if (sectorIndex < 0 || sectorIndex >= FortuneWheelArgs.SectorCount)
            {
                Debug.LogError($"[{nameof(FortuneWheelView)}] Sector index {sectorIndex} is out of range.");
                return false;
            }

            StopSpinAnimation();

            var currentZ = NormalizeAngle(_wheelRoot.localEulerAngles.z);
            var targetZ = NormalizeAngle(GetTargetWheelAngle(sectorIndex));
            var deltaToTarget = Mathf.Repeat(targetZ - currentZ + FullCircle, FullCircle);
            var fullTurns = Mathf.Max(1, _spinFullTurns);
            var endZ = currentZ + (fullTurns * FullCircle) + deltaToTarget;

            _spinTween = _wheelRoot
                .DOLocalRotate(new Vector3(0f, 0f, endZ), Mathf.Max(MinSpinDuration, _spinDuration), RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuart)
                .OnComplete(() =>
                {
                    _spinTween = null;
                    _wheelRoot.localEulerAngles = new Vector3(0f, 0f, targetZ);
                    onComplete?.Invoke();
                })
                .OnKill(() => { _spinTween = null; });

            return true;
        }

        public void StopSpinAnimation()
        {
            if (_spinTween == null)
            {
                return;
            }

            _spinTween.Kill(false);
            _spinTween = null;
        }

        private void OnDisable()
        {
            StopSpinAnimation();
        }

        protected override void OnDestroy()
        {
            _spinButton.onClick.RemoveListener(HandleSpinClick);
            _spinAdButton.onClick.RemoveListener(HandleSpinAdClick);

            StopSpinAnimation();
            base.OnDestroy();
        }

        private void SetSectors(IReadOnlyList<FortuneWheelSectorArgs> sectors)
        {
            if (sectors == null)
            {
                Debug.LogError($"[{nameof(FortuneWheelView)}] Sectors collection is null.");
                ClearSectors();
                return;
            }

            if (sectors.Count != FortuneWheelArgs.SectorCount)
            {
                Debug.LogError($"[{nameof(FortuneWheelView)}] Wheel expects exactly {FortuneWheelArgs.SectorCount} sectors.");
            }

            var count = Mathf.Min(sectors.Count, _sectors.Length);
            for (var i = 0; i < count; i++)
            {
                var sectorData = sectors[i];
                if (sectorData == null)
                {
                    _sectors[i].Clear();
                    continue;
                }

                _sectors[i].SetData(sectorData);
            }

            for (var i = count; i < _sectors.Length; i++)
            {
                _sectors[i].Clear();
            }
        }

        private void ClearSectors()
        {
            for (var i = 0; i < _sectors.Length; i++)
            {
                _sectors[i].Clear();
            }
        }

        private void HandleSpinClick()
        {
            SpinClick?.Invoke();
        }

        private void HandleSpinAdClick()
        {
            SpinAdClick?.Invoke();
        }

        private void UpdateTimerLabel(TimeSpan remainingTime)
        {
            if (_timerText == null)
            {
                return;
            }

            _timerText.text = FormatTime(remainingTime);
        }

        private static float GetTargetWheelAngle(int sectorIndex)
        {
            return PointerAngle - (sectorIndex * SectorAngle);
        }

        private static float NormalizeAngle(float angle)
        {
            return Mathf.Repeat(angle, FullCircle);
        }

        private static string FormatTime(TimeSpan remaining)
        {
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            if (remaining.TotalDays >= 1)
            {
                return $"{remaining.Days}d {remaining.Hours}h";
            }

            return $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }
    }
}
