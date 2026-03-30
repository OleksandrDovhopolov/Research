using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration;
using Infrastructure;
using TMPro;
using UIShared;
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
        [SerializeField] private Sprite _fallbackSprite;

        private CancellationToken _ct;
        private string _spriteAddress;
        
        private void Start()
        {
            _ct = this.GetCancellationTokenOnDestroy();
        }
        
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
        
        public async UniTask LoadCollectionSprite(string spriteAddress)
        {
            _spriteAddress =  spriteAddress;
            Sprite sprite = null;
            try
            {
                sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(_spriteAddress, _ct);
            }
            catch (Exception loadPrimaryException)
            {
                Debug.LogWarning($"Failed to load sprite for Sprite='{_spriteAddress}'. Falling back to default. {loadPrimaryException.Message}");
            }

            SetCollectionImage(sprite);
        }

        public void Release()
        {
            _collectionImage = null;
            ProdAddressablesWrapper.Release(_spriteAddress);
            _spriteAddress =  null;
        }
    }
}