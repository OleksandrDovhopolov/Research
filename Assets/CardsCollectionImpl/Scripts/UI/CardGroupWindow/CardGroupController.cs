using System;
using System.Collections.Generic;
using System.Linq;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using VContainer;

namespace CardCollectionImpl
{
    public class CardGroupArgs : WindowArgs
    {
        public readonly string EventId;
        public readonly string GroupType;
        public readonly IReadOnlyList<CardConfig> Cards;
        public readonly IReadOnlyList<CardCollectionGroupConfig> Groups;
        public readonly CardCollectionNewCardsDto NewCardsData;
        public readonly EventCardsSaveData EventCardsSaveData;
        public readonly CardCollectionRewardsConfigSO RewardsConfigSo;
        public readonly Action<string> OnGroupChanged;
        
        public CardGroupArgs(
            string  eventId,
            CardCollectionNewCardsDto newCardsData, 
            EventCardsSaveData eventCardsSaveData,
            string groupType,
            IReadOnlyList<CardConfig> cards,
            IReadOnlyList<CardCollectionGroupConfig> groups,
            CardCollectionRewardsConfigSO rewardsConfigSo,
            Action<string> onGroupChanged)
        {
            EventId = eventId;
            GroupType = groupType;
            Cards = cards;
            Groups = groups;
            NewCardsData = newCardsData;
            EventCardsSaveData = eventCardsSaveData;
            RewardsConfigSo = rewardsConfigSo;
            OnGroupChanged = onGroupChanged;
        }
    }
    
    [Window("CardGroupWindow", WindowType.Popup)]
    public class CardGroupController :  WindowController<CardGroupView>
    {
        private ICardCollectionCacheService _cardCollectionCardCollectionCacheService;
        private IEventSpriteManager _eventSpriteManager;
        
        private CardGroupArgs Args => (CardGroupArgs) Arguments;

        private List<CardProgressData> GroupCardsData => _cardCollectionCardCollectionCacheService.GetCardsByGroupType(Args.EventCardsSaveData, _currentGroupType).ToList();

        private IReadOnlyList<CardCollectionGroupConfig> CollectionGroups => Args.Groups;
        private int _currentGroupIndex;
        private string _currentGroupType;
        private string _lastRenderedEventId;
        private CardCollectionRewardsConfigSO _activeRewardsConfigSo;
        
        [Inject]
        private void Construct(
            ICardCollectionCacheService cardCollectionCardCollectionCacheService,
            IEventSpriteManager eventSpriteManager)
        {
            _cardCollectionCardCollectionCacheService = cardCollectionCardCollectionCacheService;
            _eventSpriteManager = eventSpriteManager;
        }
        
        protected override void OnShowStart()
        {
            var currentEventId = Args.EventId;
            if (!string.Equals(_lastRenderedEventId, currentEventId, StringComparison.Ordinal))
            {
                View.DisableAll();
                _lastRenderedEventId = currentEventId;
            }

            _activeRewardsConfigSo = Args.RewardsConfigSo;
            _currentGroupType = Args.GroupType;
            
            _currentGroupIndex = -1;
            for (var i = 0; i < CollectionGroups.Count; i++)
            {
                if (CollectionGroups[i].groupType != _currentGroupType) continue;
                _currentGroupIndex = i;
                break;
            }

            View.SetCardConfigs(Args.EventId, Args.Cards);
            View.SetSpriteManager(_eventSpriteManager);
            ShowCurrentGroup();
        }
        
        private void ShowCurrentGroup()
        {
            SetRewardData();
            
            View.CreateDataViews(_currentGroupType, GroupCardsData, Args.NewCardsData);
            
            UpdateGroupViewData();
            UpdateCardSprites();
            MarkCurrentGroupAsSeen();
        }

        private void SetRewardData()
        {
            var rewardViewData = UIUtils.CreateRewardViewData(_activeRewardsConfigSo, _currentGroupType);
            View.SetRewardData(rewardViewData.Icon, rewardViewData.Amount);
        }
        
        private async UniTask SetCardSprites(List<CardConfig> cardsData)
        {
            await View.SetSprites(Args.EventId, cardsData);
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.OnLeftClick += OnLeftClickHandler;
            View.OnRightClick += OnRightClickHandler;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            View.OnLeftClick -= OnLeftClickHandler;
            View.OnRightClick -= OnRightClickHandler;
            View.DisableAll();
        }

        private void OnLeftClickHandler()
        {
            SwitchGroup(-1).Forget();
        }

        private void OnRightClickHandler()
        {
            SwitchGroup(1).Forget();
        }
        
        private async UniTask SwitchGroup(int direction)
        {
            if (View.IsAnimating) return;

            _currentGroupIndex = (_currentGroupIndex + direction + CollectionGroups.Count) % CollectionGroups.Count;
            _currentGroupType = CollectionGroups[_currentGroupIndex].groupType;
            
            await View.AnimateSwitchGroup(direction, _currentGroupType, GroupCardsData, Args.NewCardsData, UpdateGroupViewData);

            UpdateCardSprites();
            MarkCurrentGroupAsSeen();
            Args.OnGroupChanged?.Invoke(_currentGroupType);
        }

        private void MarkCurrentGroupAsSeen()
        {
            Args.NewCardsData.MarkGroupAsSeen(_currentGroupType);
        }

        private void UpdateCardSprites()
        {
            var configs = Args.Cards.GetByGroupType(_currentGroupType);
            SetCardSprites(configs).Forget();
        }
        
        private void UpdateGroupViewData()
        {
            SetRewardData();
            
            var collectionNumberText = "Set " + (_currentGroupIndex + 1) + "/" + CollectionGroups.Count;
            View.SetCollectionNumber(collectionNumberText);

            var collectedAmount =  _cardCollectionCardCollectionCacheService.GetCollectedGroupAmount(Args.EventCardsSaveData, _currentGroupType);;
            var totalAmount = _cardCollectionCardCollectionCacheService.GetGroupAmount(Args.EventCardsSaveData, _currentGroupType);
            View.UpdateCollectedAmount(collectedAmount, totalAmount);
        }
        
        protected override void OnHideComplete(bool isClosed) 
        {
            View.DisableAll();
        }
        
        private void CloseWindow()
        {
            UIManager.Hide<CardGroupController>();
        }
    }
}