using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public class SelectedCardView : MonoBehaviour
    {
        [Space, Space, Header("SelectedCardAnimation")] 
        [SerializeField] private GameObject _selectedCardContainer;
        [SerializeField] private Button _selectedCardBackgroundButton;
        [SerializeField] private AnimatedCardView _selectedCardView;
        [SerializeField] private RectTransform _targetRect;
        
        private CollectionCardView _clickedCardView;
        
        public void OnCardPressedHandler(CollectionCardView cardView, CardConfig config)
        {
            _clickedCardView = cardView;
            
            _clickedCardView.OnCardAnimationStarted();
            _selectedCardContainer.gameObject.SetActive(true);

            _selectedCardView.SetConfig(config);
            _selectedCardView.UpdateCardFrame();
            _selectedCardView.SetOpenCardContainerActive(cardView.IsOpen);
            _selectedCardView.SetClosedCardContainerActive(!cardView.IsOpen);
            _selectedCardView.SetCardNew(cardView.IsNew);
            
            UniTaskUtils.DelayCallbackAsync(SelectedCardCallback, _selectedCardView.AnimationDuration).Forget();

            _selectedCardView.GetRectTransform().position = _clickedCardView.GetRectTransform().position;
            _selectedCardView.UpdateCardName();
            _selectedCardView.UpdateCardStars();
            
            SetSprite().Forget();
            _selectedCardView.PlayCardPreview(_targetRect.localPosition);

            async UniTask SetSprite()
            {
                var ct = this.GetCancellationTokenOnDestroy();
                ct.ThrowIfCancellationRequested();
                var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(config.icon, ct);
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
            
            var sourceRect = _clickedCardView.GetRectTransform();
            var targetRect = _selectedCardView.GetRectTransform();
            var targetPosition = UIUtils.ConvertWorldToLocalOfTargetParent(sourceRect, targetRect);
            _selectedCardView.HideCard(targetPosition);
            
            UniTaskUtils.DelayCallbackAsync(() =>
            {
                _clickedCardView.OnCardAnimationCompleted();
                _selectedCardContainer.gameObject.SetActive(false); 
            }, _selectedCardView.AnimationDuration).Forget();
        }
    }
}