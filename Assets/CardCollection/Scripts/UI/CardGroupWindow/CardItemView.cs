using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public partial class CardItemView : MonoBehaviour
    {
        private const int FiveStarsCount = 5;
        
        [Space][Space]
        [Header("Base")]
        [SerializeField] private GameObject _futureCardContainer;
        [SerializeField] private GameObject _collectedCardContainer;
        [SerializeField] private TextMeshProUGUI _cardNameText;
        [SerializeField] private TextMeshProUGUI _futureCardNameText;
        [SerializeField] private Image _cardImage;

        public void Configure(Sprite sprite, string cardName, bool collected, int starsCount)
        {
            SetCardSprite(sprite);
            SetContainersActive(collected);
            SetCardName(cardName);
            SetStars(starsCount);
        }

        public void Configure(CardModel cardModel)
        {
            SetCardSprite(cardModel.Sprite);
            SetContainersActive(cardModel.Collected);
            SetCardName(cardModel.CardName);
            SetStars(cardModel.StarsCount);
        }
        
        private void SetCardSprite(Sprite sprite)
        {
            _cardImage.sprite  = sprite;
        }
        
        private void SetCardName(string cardName)
        {
            _cardNameText.text = cardName;
            _futureCardNameText.text = cardName;
        }
        
        private void SetContainersActive(bool isCollected)
        {
            if (_collectedCardContainer != null)
                _collectedCardContainer.SetActive(isCollected);

            if (_futureCardContainer != null)
                _futureCardContainer.SetActive(!isCollected);
        }
    }
}