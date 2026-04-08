using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
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
        
        private CardView _clickedCardView;
        private IEventSpriteManager _eventSpriteManager;

        public void SetSpriteManager(IEventSpriteManager eventSpriteManager)
        {
            _eventSpriteManager = eventSpriteManager;
        }
        
        public void OnCardPressedHandler(string eventId, CardView cardView, CardConfig config)
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
                var spriteAddress = eventId + "_" + config.icon;
                await _eventSpriteManager.BindSpriteFromAtlasAsync(eventId, eventId, spriteAddress, _selectedCardView.GetCardImage(), ct);
            }
        }

        public void ReleaseSprite()
        {
            _selectedCardView.SetCardImage(null, true);
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

        public void HideImmediately()
        {
            _selectedCardBackgroundButton.onClick.RemoveAllListeners();
            if (_clickedCardView != null)
            {
                _clickedCardView.OnCardAnimationCompleted();
                _clickedCardView = null;
            }

            _selectedCardContainer.gameObject.SetActive(false);
        }
    }
}