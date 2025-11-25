using UnityEngine;

namespace core
{
    public partial class CardItemView
    {
        [Space][Space]
        [Header("Stars")]
        [SerializeField] private GameObject[] _otherStarsContainer; 
        [SerializeField] private GameObject _fiveStarsContainer;        
        
        private void SetStars(int starsCount)
        {
            if (starsCount < 1) starsCount = 1;
            if (starsCount > FiveStarsCount) starsCount = FiveStarsCount;
            
            if (starsCount == FiveStarsCount)
            {
                _fiveStarsContainer.SetActive(true);
                foreach (var star in _otherStarsContainer)
                {
                    star.SetActive(false);
                }
            }
            else
            {
                _fiveStarsContainer.SetActive(false);
                for (var i = 0; i < _otherStarsContainer.Length; i++)
                {
                    _otherStarsContainer[i].SetActive(i < starsCount);
                }
            }
        }
    }
}