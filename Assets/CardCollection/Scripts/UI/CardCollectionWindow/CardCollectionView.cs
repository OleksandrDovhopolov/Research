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
        [SerializeField] private GameObject _loadingAnimationObject;
        
        private readonly Dictionary<string, CardsCollectionView> _viewsDict = new();
        
        public event Action<string> OnGroupButtonPressed;
        
        protected override void Awake()
        {
            base.Awake();
        
            _openGroupWindowButton.onClick.AddListener(OnButtonClicked);
        }

        private bool _groupsCreated;

        public void CreateViews(List<CardGroupsConfig> groupsData)
        {
            _cardGroupsPool.DisableNonActive();

            _viewsDict.Clear();
            
            foreach (var groupsConfig in groupsData)
            {
                var groupView = _cardGroupsPool.GetNext();
                
                var groupType = groupsConfig.GroupType;
                var groupName = groupsConfig.GroupName;
                var collectedGroupAmount = 0;
                
                groupView.SetData(groupType, groupName, collectedGroupAmount.ToString());
                groupView.OnButtonPressed += OnButtonPressedHandler;
                _viewsDict.Add(groupsConfig.Id, groupView);
            }
        }
        
        public async UniTask CreateGroupViews(List<CardGroupsConfig> groupsData)
        {
            var loadTasks = groupsData.Select(async config => {
                    try 
                    {
                        var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(config.GroupIcon);
                        if (_viewsDict.TryGetValue(config.Id, out var view))
                            view.SetSprite(sprite);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed sprite {config.GroupIcon}: {e}");
                    }
            });
                
            await UniTask.WhenAll(loadTasks);
            await UniTask.WaitForSeconds(0.5f);
        }
        
        private void OnButtonPressedHandler(string groupType)
        {
            OnGroupButtonPressed?.Invoke(groupType);
        }
        
        public void ShowLoader(bool show)
        {
            _loadingAnimationObject.gameObject.SetActive(show);
        }
        
        private void OnButtonClicked()
        {
            OnButtonPressed?.Invoke();
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (var view in _viewsDict.Values)
            {
                view.OnButtonPressed -= OnButtonPressedHandler;
            }
            
            _openGroupWindowButton.onClick.AddListener(OnButtonClicked);
        }
    }
}