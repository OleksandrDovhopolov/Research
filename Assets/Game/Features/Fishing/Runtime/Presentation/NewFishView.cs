using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using TMPro;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Fishing
{
    public class NewFishView : WindowView
    {
        [SerializeField] private Image _fishImage;
        [SerializeField] private GameObject _isNewFishGameObject;
        [SerializeField] private TextMeshProUGUI _caughtWeightText;
        [SerializeField] private TextMeshProUGUI _bestCaughtWeightText;

        [Space, Header("Stars")]
        [SerializeField] private GameObject _commonCollectedObject;
        [SerializeField] private GameObject _rareCollectedObject;
        [SerializeField] private GameObject _epicCollectedObject;
        [SerializeField] private GameObject _legendaryCollectedObject;

        private FishCollectionItemView _cachedFishCardView;

        public void Render(NewFishArgs args)
        {
            if (args == null)
            {
                SetIsNewFish(false);
                SetText(_caughtWeightText, string.Empty);
                SetText(_bestCaughtWeightText, string.Empty);
                ApplyCollectedStates(Array.Empty<string>());
                return;
            }

            LoadFishSprite(args.FishId, this.GetCancellationTokenOnDestroy()).Forget();

            SetIsNewFish(args.IsNew);
            SetText(_caughtWeightText, FormatWeight(args.CaughtWeight));
            SetText(_bestCaughtWeightText, FormatWeight(args.BestCaughtWeight));
            ApplyCollectedStates(args.UnlockedWeightStates);
        }

        private async UniTaskVoid LoadFishSprite(string spriteAddress, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(spriteAddress))
                return;

            try
            {
                var fishSprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(spriteAddress, ct);
                
                if (_fishImage != null)
                    _fishImage.sprite = fishSprite;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[NewFishView] Failed to load fish sprite '{spriteAddress}'. {exception}");
            }
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label == null)
                return;

            label.text = value ?? string.Empty;
        }
        
        public void SetIsNewFish(bool isNewFish)
        {
            if (_isNewFishGameObject != null)
                _isNewFishGameObject.SetActive(isNewFish);
        }

        private void ApplyCollectedStates(IReadOnlyList<string> unlockedWeightStates)
        {
            SetCollectedState(_commonCollectedObject, HasUnlockedState(unlockedWeightStates, "common"));
            SetCollectedState(_rareCollectedObject, HasUnlockedState(unlockedWeightStates, "rare"));
            SetCollectedState(_epicCollectedObject, HasUnlockedState(unlockedWeightStates, "epic"));
            SetCollectedState(_legendaryCollectedObject, HasUnlockedState(unlockedWeightStates, "legendary"));
        }

        private static bool HasUnlockedState(IReadOnlyList<string> unlockedWeightStates, string targetStateId)
        {
            if (unlockedWeightStates == null || string.IsNullOrWhiteSpace(targetStateId))
                return false;

            for (var i = 0; i < unlockedWeightStates.Count; i++)
            {
                if (string.Equals(unlockedWeightStates[i], targetStateId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string FormatWeight(float weight)
        {
            return weight <= 0f ? string.Empty : FishCollectionDataBuilder.FormatWeight(weight);
        }

        private static void SetCollectedState(GameObject target, bool isActive)
        {
            if (target != null)
                target.SetActive(isActive);
        }
    }
}
