using EventOrchestration;
using GameplayUI;
using TMPro;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public class CollectionStartedView : WindowView
    {
        [SerializeField] private Image _collectionImage;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private EventTimerDisplay _timer;
        
        public void SetTimer(string eventId, IGlobalTimerService globalTimerService)
        {
            _timer.Bind(eventId, globalTimerService);
        }

        public void RemoveTimer()
        {
            _timer.Unbind();
        }
        
        public void SetDescription(string collectionName)
        {
            _titleText.text = collectionName;
            _descriptionText.text = $"The {collectionName} has started!\nAssemble sets of cards to win amazing rewards!";
        }
        
        public void SetCollectionImage(Sprite sprite)
        {
            _collectionImage.sprite = sprite;
        }

        public void Release()
        {
            _collectionImage.sprite = null;
        }
    }
}