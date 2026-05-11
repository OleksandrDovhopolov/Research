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
        [SerializeField] private TextMeshProUGUI _bestCaughtWeightText;
        
        [Space, Header("Stars")]
        [SerializeField] private GameObject _commonCollectedObject;
        [SerializeField] private GameObject _rareCollectedObject;
        [SerializeField] private GameObject _epicCollectedObject;
        [SerializeField] private GameObject _legendaryCollectedObject;
        
        public void Render(string fishId, float bestCaughtWeight, bool argsIsNew)
        {
            //TODO fix token logic 
            LoadFishSprite(fishId, this.GetCancellationTokenOnDestroy()).Forget();
            SetIsNewFish(argsIsNew);
            SetText(_bestCaughtWeightText, bestCaughtWeight <= 0f ? string.Empty : FishCollectionDataBuilder.FormatWeight(bestCaughtWeight));
        }

        private async UniTask LoadFishSprite(string spriteAddress, CancellationToken ct)
        {
            var fishSprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(spriteAddress, ct);
            _fishImage.sprite = fishSprite;
        }
        
        private static void SetText(TMP_Text label, string value)
        {
            if (label == null)
                return;

            label.text = value ?? string.Empty;
        }
        
        public void SetIsNewFish(bool isNewFish)
        {
            _isNewFishGameObject.gameObject.SetActive(isNewFish);
        }
    }
}
