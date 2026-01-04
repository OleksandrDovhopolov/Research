using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class CollectionCardView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _cardName;
        [SerializeField] private Image _cardImage;
        
        [SerializeField] private GameObject star1;
        [SerializeField] private GameObject star2;
        [SerializeField] private GameObject star3;
        [SerializeField] private GameObject star4;
        [SerializeField] private GameObject star5;
        
        public void SetCardName(string cardName)
        {
            _cardName.text = cardName;
        }
        
        public void SetStars(int starsCount)
        {
            starsCount = Mathf.Clamp(starsCount, 1, 5);
            
            star1.SetActive(false);
            star2.SetActive(false);
            star3.SetActive(false);
            star4.SetActive(false);
            star5.SetActive(false);

            if (starsCount == 5)
            {
                star5.SetActive(true);
            }
            else
            {
                if (starsCount >= 1) star1.SetActive(true);
                if (starsCount >= 2) star2.SetActive(true);
                if (starsCount >= 3) star3.SetActive(true);
                if (starsCount >= 4) star4.SetActive(true);
            }            
        }
        
        public void SetCardImage(Sprite cardSprite)
        {
            _cardImage.sprite = cardSprite;
        }
    }
}