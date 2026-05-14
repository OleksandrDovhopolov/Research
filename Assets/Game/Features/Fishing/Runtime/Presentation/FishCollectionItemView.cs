using TMPro;
using UIShared;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Game.Fishing
{
    public sealed class FishCollectionItemView : MonoBehaviour, ICleanup
    {
        private const string CommonStateId = "common";
        private const string RareStateId = "rare";
        private const string EpicStateId = "epic";
        private const string LegendaryStateId = "legendary";
        private static readonly Color32 DiscoveredIconColor = new(255, 255, 255, 255);
        private static readonly Color32 UndiscoveredIconColor = new(0, 0, 0, 125);

        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _displayNameText;
        [SerializeField] private TextMeshProUGUI _waterBodyTypeText;
        [SerializeField] private TextMeshProUGUI _behaviorTypeText;
        [SerializeField] private TextMeshProUGUI _minWeightText;
        [SerializeField] private TextMeshProUGUI _maxWeightText;
        [SerializeField] private TextMeshProUGUI _bestCaughtWeightText;
        [SerializeField] private UIListPool<FishCollectionLureIconView> _lureIconsPool;
        [SerializeField] private GameObject _commonCollectedObject;
        [SerializeField] private GameObject _rareCollectedObject;
        [SerializeField] private GameObject _epicCollectedObject;
        [SerializeField] private GameObject _legendaryCollectedObject;

        public string SpriteAddress { get; private set; }
        public IReadOnlyList<string> LureSpriteAddresses => _lureSpriteAddresses;

        private readonly List<string> _lureSpriteAddresses = new();

        public void SetData(FishCollectionEntryViewData data)
        {
            SpriteAddress = data?.SpriteAddress ?? string.Empty;
            _lureSpriteAddresses.Clear();
            SetText(_displayNameText, data?.DisplayName);
            SetText(_waterBodyTypeText, data?.WaterBodyTypesText);
            SetText(_behaviorTypeText, data?.BehaviorType);
            SetText(_minWeightText, data == null ? string.Empty : FishCollectionDataBuilder.FormatWeight(data.MinWeight));
            SetText(_maxWeightText, data == null ? string.Empty : FishCollectionDataBuilder.FormatWeight(data.MaxWeight));
            SetText(_bestCaughtWeightText, data == null || data.BestCaughtWeight <= 0f ? string.Empty : FishCollectionDataBuilder.FormatWeight(data.BestCaughtWeight));
            ApplyDiscoveryState(data?.IsDiscovered == true);
            ApplyLures(data?.Lures);
            ApplyCollectedStates(data?.Progress);
        }

        public void SetSprite(Sprite sprite)
        {
            if (_icon != null)
                _icon.sprite = sprite;
        }

        public void SetLureSprite(string spriteAddress, Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(spriteAddress) || _lureIconsPool == null)
                return;

            foreach (var lureIconView in _lureIconsPool.ActiveElements())
            {
                if (lureIconView != null && lureIconView.SpriteAddress == spriteAddress)
                    lureIconView.SetSprite(sprite);
            }
        }

        public void Cleanup()
        {
            SpriteAddress = string.Empty;
            _lureSpriteAddresses.Clear();

            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.color = DiscoveredIconColor;
            }

            SetText(_displayNameText, string.Empty);
            SetText(_waterBodyTypeText, string.Empty);
            SetText(_behaviorTypeText, string.Empty);
            SetText(_minWeightText, string.Empty);
            SetText(_maxWeightText, string.Empty);
            SetText(_bestCaughtWeightText, string.Empty);
            _lureIconsPool?.DisableAll();
            ApplyCollectedStates(null);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label == null)
                return;

            label.text = value ?? string.Empty;
        }

        private void ApplyCollectedStates(FishBookProgress progress)
        {
            SetCollectedState(_commonCollectedObject, HasUnlockedState(progress, CommonStateId));
            SetCollectedState(_rareCollectedObject, HasUnlockedState(progress, RareStateId));
            SetCollectedState(_epicCollectedObject, HasUnlockedState(progress, EpicStateId));
            SetCollectedState(_legendaryCollectedObject, HasUnlockedState(progress, LegendaryStateId));
        }

        private void ApplyDiscoveryState(bool isDiscovered)
        {
            if (_icon != null)
                _icon.color = isDiscovered ? DiscoveredIconColor : UndiscoveredIconColor;
        }

        private void ApplyLures(IReadOnlyList<FishCollectionLureViewData> lures)
        {
            _lureIconsPool?.DisableAll();
            if (_lureIconsPool == null || lures == null || lures.Count == 0)
                return;

            for (var i = 0; i < lures.Count; i++)
            {
                var lure = lures[i];
                if (lure == null || string.IsNullOrWhiteSpace(lure.SpriteAddress))
                    continue;

                _lureSpriteAddresses.Add(lure.SpriteAddress);
                var lureIconView = _lureIconsPool.GetNext();
                lureIconView.transform.SetSiblingIndex(i);
                lureIconView.SetData(lure.SpriteAddress);
            }
        }

        private static bool HasUnlockedState(FishBookProgress progress, string stateId)
        {
            return progress?.UnlockedWeightStates != null && progress.UnlockedWeightStates.Contains(stateId);
        }

        private static void SetCollectedState(GameObject target, bool isActive)
        {
            if (target != null)
                target.SetActive(isActive);
        }
    }
}
