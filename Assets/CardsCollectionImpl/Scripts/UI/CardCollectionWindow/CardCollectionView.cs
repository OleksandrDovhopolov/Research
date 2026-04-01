using System;
using System.Collections.Generic;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration;
using TMPro;
using UIShared;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public class CardCollectionView : WindowView
    {
        [SerializeField] private UIListPool<CardsCollectionView> _cardGroupsPool;
        [SerializeField] private GameObject _loadingAnimationObject;
        [SerializeField] private Button _infoButton;
        
        [Header("Points Container")]
        [SerializeField] private CardsCollectionPointsView _cardsCollectionPointsView;
        
        [Space, Space, Header("GroupReward")]
        [SerializeField] private CollectedAmountProgressView _collectedAmountProgressView;
        [SerializeField] private Button _collectionRewardButton;
        [SerializeField] private RectTransform _collectionRewardButtonRect;
        
        [Header("Points Container")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private EventTimerDisplay _eventTimerDisplay;
        
        [Space, Space, Header("CardsContentWidget")]
        [SerializeField] private CardsOfferWidgetView _inventoryWidgetView;
        
        private readonly Dictionary<string, CardsCollectionView> _viewsDict = new();
        private IEventSpriteManager _eventSpriteManager;
        
        public event Action<string> OnGroupButtonPressed;
        public event Action OnPointsViewClicked;
        public event Action OnInfoButtonClicked;
        public event Action<RectTransform> OnRewardChestClicked;
        
        private bool _groupsCreated;

        protected override void Awake()
        {
            base.Awake();
            WidgetRegistry.Register<ContentWidgetData>(_inventoryWidgetView);
        }
        
        private void Start()
        {
            _cardsCollectionPointsView.OnViewClicked += OnPointsViewClickedHandler;
            _collectionRewardButton.onClick.AddListener(OnRewardChestClickedHandler);
            _infoButton.onClick.AddListener(OnInfoButtonClickedHandler);
        }

        private void OnPointsViewClickedHandler()
        {
            OnPointsViewClicked?.Invoke();
        }
        
        private void OnRewardChestClickedHandler()
        {
            OnRewardChestClicked?.Invoke(_collectionRewardButtonRect);
        }
        
        private void OnInfoButtonClickedHandler()
        {
            OnInfoButtonClicked?.Invoke();
        }

        //TODO remove this
        private ICardCollectionCacheService _cardCollectionCardCollectionCacheService;
        public void SetService(ICardCollectionCacheService cardCollectionCacheService)
        {
            _cardCollectionCardCollectionCacheService = cardCollectionCacheService;
        }

        public void SetSpriteManager(IEventSpriteManager eventSpriteManager)
        {
            _eventSpriteManager = eventSpriteManager;
        }
        
        public void CreateViews(CardCollectionNewCardsDto newCardsData, IReadOnlyList<CardCollectionGroupConfig> configs, ICardCollectionRewardHandler rewardHandler)
        {
            _cardGroupsPool.DisableNonActive();

            _viewsDict.Clear();
            
            foreach (var groupsConfig in configs)
            {
                var groupView = _cardGroupsPool.GetNext();
                
                var groupType = groupsConfig.groupType;
                var groupName = groupsConfig.groupName;
                
                groupView.SetData(groupType, groupName);
                var rewardViewData = rewardHandler.CreateRewardViewData(groupType);
                groupView.SetRewardData(rewardViewData.Icon, rewardViewData.Amount);
                
                groupView.OnButtonPressed += OnButtonPressedHandler;
                
                _viewsDict.Add(groupsConfig.groupType, groupView);
                
                var newCardsAmount = newCardsData.GetNewGroupAmount(groupType);
                UpdateGroupNewCards(groupType, newCardsAmount);
            }
        }

        public void SetGroupsProgress(IReadOnlyList<CollectionProgressSnapshot.GroupProgressSnapshot> collectionData)
        {
            foreach (var groupProgressSnapshot in collectionData)
            {
                if (_viewsDict.TryGetValue(groupProgressSnapshot.GroupType, out var groupView))
                {
                    if (groupProgressSnapshot.CollectedAmount == groupProgressSnapshot.TotalAmount)
                    {
                        groupView.SetGroupCompleted(true);
                    }
                    else
                    {
                        groupView.SetGroupCompleted(false);
                        groupView.SetCollectedAmountProgressStart(groupProgressSnapshot.CollectedAmount, groupProgressSnapshot.TotalAmount);
                    }
                }
            }
        }

        public void UpdateGroupsProgressAnimated(EventCardsSaveData collectionData)
        {
            foreach (var groupView in _viewsDict.Values)
            {
                var groupType = groupView.GroupType;
                var totalGroupAmount = _cardCollectionCardCollectionCacheService.GetGroupAmount(collectionData, groupType);
                var collectedGroupAmount = _cardCollectionCardCollectionCacheService.GetCollectedGroupAmount(collectionData, groupType);;

                UpdateGroupProgressAnimated(groupView, collectedGroupAmount,  totalGroupAmount);
            }
        }

        public void UpdateGroupProgressAnimated(CardsCollectionView groupView, int collectedGroupAmount, int totalGroupAmount)
        {
            if (collectedGroupAmount == totalGroupAmount)
            {
                groupView.SetGroupCompleted(true);
            }
            else
            {
                groupView.UpdateCollectedAmountAnimated(collectedGroupAmount, totalGroupAmount);
            }
        }

        public void UpdateGroupProgressAnimated(EventCardsSaveData collectionData, CardConfig cardConfig)
        {
            
        }
        
        public void UpdateViews(CardCollectionNewCardsDto newCardsData)
        {
            foreach (var groupView in _viewsDict.Values)
            {
                var groupType = groupView.GroupType;
                var newCardsAmount = newCardsData.GetNewGroupAmount(groupType);
                UpdateGroupNewCards(groupType, newCardsAmount);
            }
        }

        public void BindEventTimerDisplay(IGlobalTimerService globalTimerService, string scheduleItemEventId)
        {
            if (globalTimerService == null || string.IsNullOrEmpty(scheduleItemEventId) || _eventTimerDisplay == null)
                return;

            _eventTimerDisplay.Bind(scheduleItemEventId, globalTimerService);
        }

        public void UnbindEventTimerDisplay()
        {
            _eventTimerDisplay?.Unbind();
        }
        
        public void UpdateGroupNewCards(string groupType, int groupAmount)
        {
            if (_viewsDict.TryGetValue(groupType, out var view))
            {
                view.UpdateNewCards(groupAmount);
            }
        }
        
        public async UniTask CreateGroupViews(
            string argsScheduleItemEventId,
            IReadOnlyList<CardCollectionGroupConfig> groupsData)
        {
            await UIUtils.BindAndSetSpritesAsync(
                groupsData,
                argsScheduleItemEventId,
                _eventSpriteManager,
                config => argsScheduleItemEventId + "/" + config.groupIcon,
                config =>
                {
                    if (_viewsDict.TryGetValue(config.groupType, out var view))
                        return view.GetGroupImage();

                    return null;
                });
        }
        
        public void UpdateGlobalCollectedAmount(int collectedAmount, int totalAmount)
        {
            _collectedAmountProgressView.UpdateCollectedAmountAnimated(collectedAmount, totalAmount);
        }

        public void SetGlobalCollectedProgressStart(int collectedAmount, int totalAmount)
        {
            _collectedAmountProgressView.SetPreviousProgress(collectedAmount, totalAmount);
        }
        
        private void OnButtonPressedHandler(string groupType)
        {
            OnGroupButtonPressed?.Invoke(groupType);
        }
        
        public void ShowLoader(bool show)
        {
            _loadingAnimationObject.gameObject.SetActive(show);
        }
        
        public void UpdatePointsAmount(int pointsAmount)
        {
            _cardsCollectionPointsView.SetPointsAmount(pointsAmount);
        }

        public void ReleaseSprites()
        {
            foreach (var kvp in _viewsDict)
            {
                kvp.Value.SetSprite(null, true);
            }
        }
        
        protected override void OnDestroy()
        {
            UnbindEventTimerDisplay();

            base.OnDestroy();

            _cardsCollectionPointsView.OnViewClicked -= OnPointsViewClickedHandler;
            _collectionRewardButton.onClick.RemoveAllListeners();
            _infoButton.onClick.RemoveAllListeners();
            
            foreach (var view in _viewsDict.Values)
            {
                view.OnButtonPressed -= OnButtonPressedHandler;
            }
        }
    }
}