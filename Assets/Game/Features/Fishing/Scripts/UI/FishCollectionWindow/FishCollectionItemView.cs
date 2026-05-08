using TMPro;
using UIShared;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Fishing
{
    public sealed class FishCollectionItemView : MonoBehaviour, ICleanup
    {
        private const string CommonStateId = "common";
        private const string RareStateId = "rare";
        private const string EpicStateId = "epic";
        private const string LegendaryStateId = "legendary";

        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _displayNameText;
        [SerializeField] private TextMeshProUGUI _waterBodyTypeText;
        [SerializeField] private TextMeshProUGUI _behaviorTypeText;
        [SerializeField] private TextMeshProUGUI _minWeightText;
        [SerializeField] private TextMeshProUGUI _maxWeightText;
        [SerializeField] private GameObject _commonCollectedObject;
        [SerializeField] private GameObject _rareCollectedObject;
        [SerializeField] private GameObject _epicCollectedObject;
        [SerializeField] private GameObject _legendaryCollectedObject;

        public string SpriteAddress { get; private set; }

        public void SetData(FishCollectionEntryViewData data)
        {
            SpriteAddress = data?.SpriteAddress ?? string.Empty;
            SetText(_displayNameText, data?.DisplayName);
            SetText(_waterBodyTypeText, data?.WaterBodyTypesText);
            SetText(_behaviorTypeText, data?.BehaviorType);
            SetText(_minWeightText, data == null ? string.Empty : FishCollectionDataBuilder.FormatWeight(data.MinWeight));
            SetText(_maxWeightText, data == null ? string.Empty : FishCollectionDataBuilder.FormatWeight(data.MaxWeight));
            ApplyCollectedStates(data?.Progress);
        }

        public void SetSprite(Sprite sprite)
        {
            if (_icon != null)
                _icon.sprite = sprite;
        }

        public void Cleanup()
        {
            SpriteAddress = string.Empty;

            if (_icon != null)
                _icon.sprite = null;

            SetText(_displayNameText, string.Empty);
            SetText(_waterBodyTypeText, string.Empty);
            SetText(_behaviorTypeText, string.Empty);
            SetText(_minWeightText, string.Empty);
            SetText(_maxWeightText, string.Empty);
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
