using System;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class CardCollectionView : WindowView
    {
        public event Action OnButtonPressed;
        
        [SerializeField] private Button _openGroupWindowButton;
        [SerializeField] public Sprite Sprite;
        
        protected override void Awake()
        {
            base.Awake();
        
            _openGroupWindowButton.onClick.AddListener(OnButtonClicked);
        }
                
        private void OnButtonClicked()
        {
            OnButtonPressed?.Invoke();
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            _openGroupWindowButton.onClick.AddListener(OnButtonClicked);
        }
    }
}