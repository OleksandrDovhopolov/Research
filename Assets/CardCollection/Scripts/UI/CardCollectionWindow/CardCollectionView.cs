using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class CardCollectionView : WindowView
    {
        public event Action OnButtonPressed;
        
        [SerializeField] private Button _openGroupWindowButton;
        [SerializeField] private UIListPool<CardsCollectionView> _cardGroupsPool;
        
        private List<CardsCollectionView> _views = new();
        
        protected override void Awake()
        {
            base.Awake();
        
            _openGroupWindowButton.onClick.AddListener(OnButtonClicked);
        }

        private bool _groupsCreated;
        
        public async UniTask CreateGroupViews(List<CardGroupsConfig> groupsData)
        {
            if (_groupsCreated) return;
            _groupsCreated = true;
            
            
            Debug.LogWarning($"ShowLoader called : groupsData {groupsData.Count}");
            _cardGroupsPool.DisableNonActive();

            _views.Clear();
            
            foreach (var groupsConfig in groupsData)
            {
                var groupSprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(groupsConfig.GroupIcon);
                 
                var groupView = _cardGroupsPool.GetNext();
                
                var groupType = groupsConfig.GroupType;
                var groupName = groupsConfig.GroupName;
                var collectedGroupAmount = 0;
                
                groupView.SetData(groupType, groupName, collectedGroupAmount.ToString(), groupSprite);
                _views.Add(groupView);
            }
        }
        
        public void ShowLoader(bool show)
        {
            Debug.LogWarning($"ShowLoader called : show {show}");
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