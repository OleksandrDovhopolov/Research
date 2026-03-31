using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UIShared;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardGroupCompletedView : WindowView
    {
        [SerializeField] private UIListPool<CardsCollectionView> _uiListPool;
        [SerializeField] private RectTransform _animationStartPosition;
        public Vector3 AnimationStartPosition => _animationStartPosition.transform.position;
        
        private readonly Dictionary<string, CardsCollectionView> _activeViewsByGroupType = new();

        public readonly struct GroupCompletedViewData
        {
            public readonly string GroupType;
            public readonly string GroupName;
            public readonly string GroupIcon;
            public readonly int CollectedGroupAmount;
            public readonly int TotalGroupAmount;
            public readonly Sprite RewardSprite;
            public readonly int RewardAmount;

            public GroupCompletedViewData(
                string groupType,
                string groupName,
                string groupIcon,
                int collectedGroupAmount,
                int totalGroupAmount,
                Sprite rewardSprite,
                int rewardAmount)
            {
                GroupType = groupType;
                GroupName = groupName;
                GroupIcon = groupIcon;
                CollectedGroupAmount = collectedGroupAmount;
                TotalGroupAmount = totalGroupAmount;
                RewardSprite = rewardSprite;
                RewardAmount = rewardAmount;
            }
        }

        public void CreateViews(string eventId, List<GroupCompletedViewData> groupsDataByType, IEventSpriteManager eventSpriteManager)
        {
            _activeViewsByGroupType.Clear();
            _uiListPool.DisableNonActive();

            if (groupsDataByType == null || groupsDataByType.Count == 0)
            {
                return;
            }

            foreach (var groupDataByType in groupsDataByType)
            {
                var groupView = _uiListPool.GetNext();
                groupView.SetData(groupDataByType.GroupType, groupDataByType.GroupName);
                groupView.SetRewardData(groupDataByType.RewardSprite, groupDataByType.RewardAmount);
                groupView.SetGroupCompleted(false);
                groupView.SetCollectedAmountProgressStart(0, groupDataByType.TotalGroupAmount);
                groupView.UpdateCollectedAmountAnimated(
                    groupDataByType.CollectedGroupAmount,
                    groupDataByType.TotalGroupAmount,
                    isGroupCompleted => OnAnimationCompletedHandler(groupView, isGroupCompleted));

                SetSprite(eventSpriteManager, groupView, eventId, groupDataByType.GroupIcon).Forget();
                _activeViewsByGroupType[groupDataByType.GroupType] = groupView;
            }
        }

        private  async UniTask SetSprite(IEventSpriteManager eventSpriteManager, CardsCollectionView view, string eventId, string spriteName)
        {
            var ct = this.GetCancellationTokenOnDestroy();
            ct.ThrowIfCancellationRequested();
            var spriteAddress = eventId + "/" + spriteName;
            await eventSpriteManager.BindSpriteAsync(eventId, spriteAddress, view.GetGroupImage(), ct);
        }
        
        private static void OnAnimationCompletedHandler(CardsCollectionView groupView, bool isGroupCompleted)
        {
            if (!isGroupCompleted)
            {
                return;
            }

            groupView.ShowCompleteCheckMark();
        }

        public bool TryGetAnimationStartPosition(string groupType, out Vector3 animationStartPosition)
        {
            if (!string.IsNullOrWhiteSpace(groupType) &&
                _activeViewsByGroupType.TryGetValue(groupType, out var groupView) &&
                groupView != null)
            {
                animationStartPosition = groupView.AnimationStartPosition;
                return true;
            }

            animationStartPosition = AnimationStartPosition;
            return false;
        }

        public void ResetView()
        {
            foreach (var groupView in _activeViewsByGroupType.Values)
            {
                if (groupView == null)
                {
                    continue;
                }

                groupView.SetCollectedAmountProgressStart(0, 0);
                groupView.SetGroupCompleted(false);
            }

            _activeViewsByGroupType.Clear();
            _uiListPool.DisableNonActive();
        }
    }
}