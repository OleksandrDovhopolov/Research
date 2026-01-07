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
        [SerializeField] private RectTransform _targetRect;
        
        private bool _isCardOpen;
        private CollectionCardView _clickedCardView;
        
        public void OnCardPressedHandler(CollectionCardView cardView, CardCollectionConfig config, bool isOpen = true)
        {
            _isCardOpen = isOpen;
            _clickedCardView = cardView;
            
            _clickedCardView.OnCardAnimationStarted();
            _selectedCardContainer.gameObject.SetActive(true); 
            
            _selectedCardView.SetOpenCardContainerActive(isOpen);
            _selectedCardView.SetClosedCardContainerActive(!isOpen);
            
            UniTaskUtils.DelayCallbackAsync(SelectedCardCallback, _selectedCardView.AnimationDuration).Forget();

            _selectedCardView.GetRectTransform(isOpen).position = _clickedCardView.GetRectTransform(isOpen).position;
            _selectedCardView.SetCardName(config.CardName);
            _selectedCardView.SetStars(config.Stars);
            
            SetSprite().Forget();
            _selectedCardView.PlayCardPreview(_targetRect.localPosition, isOpen);

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
            
            var sourceRect = _clickedCardView.GetRectTransform(_isCardOpen);
            var targetRect = _selectedCardView.GetRectTransform(_isCardOpen);
            var targetPosition = UIUtils.ConvertWorldToLocalOfTargetParent(sourceRect, targetRect);
            _selectedCardView.HideCard(targetPosition, _isCardOpen);
            
            UniTaskUtils.DelayCallbackAsync(() =>
            {
                _clickedCardView.OnCardAnimationCompleted();
                _selectedCardContainer.gameObject.SetActive(false); 
            }, _selectedCardView.AnimationDuration).Forget();
        }
    }
}