using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class CardGroupView : WindowView
    {
        [Space, Space, Header("CardsPool")]
        [SerializeField] private UIListPool<CollectionCardView> _upperCardsPool;
        [SerializeField] private UIListPool<CollectionCardView> _bottomCardsPool;

        private readonly Dictionary<CardCollectionConfig, CollectionCardView> _viewsDict = new();

        public void CreateViews(List<CardCollectionConfig> cardsData)
        {
            _upperCardsPool.DisableNonActive();
            _bottomCardsPool.DisableNonActive();
            
            for (var i = 0; i < cardsData.Count; i++)
            {
                if (i >= 10)
                {
                    Debug.LogError($"More than 10 elements");
                    break;
                }
                
                var config = cardsData[i];
                var pool = i < 5 ? _upperCardsPool : _bottomCardsPool;
        
                var cardView = pool.GetNext();
                cardView.SetCardName(config.CardName);
                cardView.SetStars(config.Stars);
                cardView.OnCardPressed += OnCardPressedHandler;
        
                _viewsDict[config] = cardView;
            }
        }
        
        [Space, Space, Header("SelectedCardAnimation")]
        [SerializeField] private GameObject _selectedCardContainer;
        [SerializeField] private Button _selectedCardBackgroundButton;
        [SerializeField] private CollectionCardView _selectedCardView;
        
        //private Vector3 _clickedCardPosition;
        private CollectionCardView _clickedCardView;
        
        private void OnCardPressedHandler(CollectionCardView cardView)
        {
            var config = GetKeyByValue(cardView);
            if (config == null)
                return;

            Debug.LogWarning($"Debug config ID {config.Id} / {config.CardName}");
            
            _clickedCardView = cardView;
            
            _selectedCardContainer.gameObject.SetActive(true); 
            _clickedCardView.SetOpenCardContainerActive(false);
            DelayCallbackAsync(SelectedCardCallback, _selectedCardView.AnimationDuration).Forget();
        
            //_clickedCardPosition = cardView.RectTransform.position;
            _selectedCardView.RectTransform.position = _clickedCardView.RectTransform.position;
            _selectedCardView.SetCardName(config.CardName);
            _selectedCardView.SetStars(config.Stars);
            
            // var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(config.Icon);
            //_selectedCardView.SetCardImage(sprite);
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
            Debug.LogWarning($"Debug SelectedCardCallback");
            _selectedCardBackgroundButton.onClick.AddListener(HideSelectedCard);
        }
        
        private void HideSelectedCard()
        {
            Debug.LogWarning($"Debug HideSelectedCard");
            _selectedCardBackgroundButton.onClick.RemoveAllListeners();
            var targetPosition = GetPosition();
            _selectedCardView.HideCard(targetPosition);
            //_selectedCardView.HideCard(_clickedCardView.transform.localPosition);
            
            DelayCallbackAsync(() =>
            {
                _clickedCardView.SetOpenCardContainerActive(true);
                _selectedCardContainer.gameObject.SetActive(false); 
            }, _selectedCardView.AnimationDuration).Forget();
        }

        private Vector2 GetPosition()
        {
            var src = _clickedCardView.RectTransform;          // откуда
            var dst = _selectedCardView.RectTransform;         // что двигаем
            var dstParent = (RectTransform)dst.parent;

            // 1. Берём экранную позицию кликнутой карточки
            // Screen Space - Overlay → cam = null
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
                null,
                src.position);

            // 2. Переводим в локальные координаты родителя selectedCard
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dstParent,
                screenPos,
                null,
                out var localPoint);

            return localPoint;
        }
        
        private async UniTaskVoid DelayCallbackAsync(Action callback, float delaySeconds)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds));
            callback?.Invoke();
        }
        
        private CardCollectionConfig GetKeyByValue(CollectionCardView value)
        {
            foreach (var pair in _viewsDict)
            {
                if (ReferenceEquals(pair.Value, value))
                    return pair.Key;
            }
        
            return null;
        }
        
        public async UniTask SetSprites(List<CardCollectionConfig> cardsData)
        {
            var loadTasks = cardsData.Select(async config => {
                try 
                {
                    var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(config.Icon);
                    if (_viewsDict.TryGetValue(config, out var view))
                        view.SetCardImage(sprite);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed sprite {config.CardName}: {e}");
                }
            });
                
            await UniTask.WhenAll(loadTasks);
            await UniTask.WaitForSeconds(2f);
        }

        /*public async UniTask SetSprite(CardCollectionConfig config)
        {
            var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(config.Icon);
            if (_viewsDict.TryGetValue(config, out var view))
                view.SetCardImage(sprite);
        }*/

        public void DisableAll()
        {
            foreach (var cardView in _viewsDict.Values)
            {
                cardView.OnCardPressed -= OnCardPressedHandler;
            }
            _viewsDict.Clear();
            
            _upperCardsPool.DisableAll();
            _bottomCardsPool.DisableAll();
        }
    }
}