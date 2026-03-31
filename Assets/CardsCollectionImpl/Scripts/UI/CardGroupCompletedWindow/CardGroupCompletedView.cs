using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardGroupCompletedView : WindowView
    {
        [SerializeField] private CardsCollectionView _cardsCollectionView;
        [SerializeField] private RectTransform _animationStartPosition;
        public Vector3 AnimationStartPosition => _animationStartPosition.transform.position;

        public void CreateViews(string groupType, string groupName, int collectedGroupAmount, int totalGroupAmount )
        {
            _cardsCollectionView.SetCollectedAmountProgressStart(0, totalGroupAmount);
            _cardsCollectionView.UpdateCollectedAmountAnimated(collectedGroupAmount, totalGroupAmount, OnAnimationCompletedHandler);
        }

        private void OnAnimationCompletedHandler(bool isGroupCompleted)
        {
            _cardsCollectionView.ShowCompleteCheckMark();
        }

        public void ResetView()
        {
            _cardsCollectionView.SetGroupCompleted(false);
        }
    }
}