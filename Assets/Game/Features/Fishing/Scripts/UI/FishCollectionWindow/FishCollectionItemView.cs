using TMPro;
using UIShared;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Fishing
{
    public sealed class FishCollectionItemView : MonoBehaviour, ICleanup
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _displayNameText;
        [SerializeField] private TextMeshProUGUI _waterBodyTypeText;
        [SerializeField] private TextMeshProUGUI _behaviorTypeText;
        [SerializeField] private TextMeshProUGUI _itemTypeText;
        [SerializeField] private TextMeshProUGUI _minWeightText;
        [SerializeField] private TextMeshProUGUI _maxWeightText;

        public string ItemId { get; private set; }

        public void SetData(FishCollectionEntryViewData data)
        {
            ItemId = data?.ItemId ?? string.Empty;
            SetText(_displayNameText, data?.DisplayName);
            SetText(_waterBodyTypeText, data?.WaterBodyTypesText);
            SetText(_behaviorTypeText, data?.BehaviorType);
            SetText(_itemTypeText, data?.ItemType);
            SetText(_minWeightText, data == null ? string.Empty : FishCollectionDataBuilder.FormatWeight(data.MinWeight));
            SetText(_maxWeightText, data == null ? string.Empty : FishCollectionDataBuilder.FormatWeight(data.MaxWeight));
        }

        public void SetSprite(Sprite sprite)
        {
            if (_icon != null)
                _icon.sprite = sprite;
        }

        public void Cleanup()
        {
            ItemId = string.Empty;

            if (_icon != null)
                _icon.sprite = null;

            SetText(_displayNameText, string.Empty);
            SetText(_waterBodyTypeText, string.Empty);
            SetText(_behaviorTypeText, string.Empty);
            SetText(_itemTypeText, string.Empty);
            SetText(_minWeightText, string.Empty);
            SetText(_maxWeightText, string.Empty);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label == null)
                return;

            label.text = value ?? string.Empty;
        }
    }
}
