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
        
        private Dictionary<string, CardsCollectionView> _viewsDict = new();
        
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
                _viewsDict.Add(groupsConfig.Id, groupView);
            }
        }
        
        public async UniTask CreateGroupViews(List<CardGroupsConfig> groupsData)
        {
            /*if (_groupsCreated) return;
            _groupsCreated = true;
            
            Debug.LogWarning($"ShowLoader called : groupsData {groupsData.Count}");
            _cardGroupsPool.DisableNonActive();

            _viewsDict.Clear();
            
            foreach (var groupsConfig in groupsData)
            {
                var groupView = _cardGroupsPool.GetNext();
                
                var groupType = groupsConfig.GroupType;
                var groupName = groupsConfig.GroupName;
                var collectedGroupAmount = 0;
                
                groupView.SetData(groupType, groupName, collectedGroupAmount.ToString(), null);
                _viewsDict.Add(groupsConfig.Id, groupView);
            }*/
            
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
            await UniTask.WaitForSeconds(2f);
        }
        
        public void ShowLoader(bool show)
        {
            _loadingAnimationObject.gameObject.SetActive(show);
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