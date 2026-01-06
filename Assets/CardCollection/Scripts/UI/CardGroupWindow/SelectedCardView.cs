using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class SelectedCardView : MonoBehaviour
    {
        [Space, Space, Header("SelectedCardAnimation")] 
        [SerializeField] private GameObject _selectedCardContainer;
        [SerializeField] private Button _selectedCardBackgroundButton;
        [SerializeField] private AnimatedCardView _selectedCardView;
        
        private CollectionCardView _clickedCardView;
        
        public void OnCardPressedHandler(CollectionCardView cardView, CardCollectionConfig config)
        {
            _clickedCardView = cardView;
            
            _selectedCardContainer.gameObject.SetActive(true); 
            _clickedCardView.SetOpenCardContainerActive(false);
            UniTaskUtils.DelayCallbackAsync(SelectedCardCallback, _selectedCardView.AnimationDuration).Forget();
        
            _selectedCardView.RectTransform.position = _clickedCardView.RectTransform.position;
            _selectedCardView.SetCardName(config.CardName);
            _selectedCardView.SetStars(config.Stars);
            
            SetSprite().Forget();
            _selectedCardView.PlayCardPreview(Vector2.zero);

            async UniTask SetSprite()
            {
                var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(config.Icon);
                _selectedCardView.SetCardImage(sprite);
            }
        }
        
        private void SelectedCardCallback()
        {
            _selectedCardBackgroundButton.onClick.AddListener(HideSelectedCard);
        }
        
        private void HideSelectedCard()
        {
            _selectedCardBackgroundButton.onClick.RemoveAllListeners();
            var targetPosition = UIUtils.ConvertWorldToLocalOfTargetParent(_clickedCardView.RectTransform, _selectedCardView.RectTransform);
            _selectedCardView.HideCard(targetPosition);
            
            UniTaskUtils.DelayCallbackAsync(() =>
            {
                _clickedCardView.SetOpenCardContainerActive(true);
                _selectedCardContainer.gameObject.SetActive(false); 
            }, _selectedCardView.AnimationDuration).Forget();
        }
    }
}