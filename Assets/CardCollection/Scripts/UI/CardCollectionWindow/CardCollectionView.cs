using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class CardCollectionView : WindowView
    {
        [SerializeField] private UIListPool<CardsCollectionView> _cardGroupsPool;
        [SerializeField] private GameObject _loadingAnimationObject;
        
        private readonly Dictionary<string, CardsCollectionView> _viewsDict = new();
        
        public event Action<string> OnGroupButtonPressed;

        private bool _groupsCreated;

        public void CreateViews(EventCardsSaveData collectionData)
        {
            _cardGroupsPool.DisableNonActive();

            _viewsDict.Clear();

            var configs = CardGroupsConfigStorage.Instance.Data;
            
            foreach (var groupsConfig in configs)
            {
                var groupView = _cardGroupsPool.GetNext();
                
                var groupType = groupsConfig.GroupType;
                var groupName = groupsConfig.GroupName;
                var totalGroupAmount = collectionData.GetGroupAmount(groupsConfig.GroupType);
                var collectedGroupAmount = collectionData.GetCollectedGroupAmount(groupsConfig.GroupType);
                
                groupView.SetData(groupType, groupName, collectedGroupAmount, totalGroupAmount);
                groupView.OnButtonPressed += OnButtonPressedHandler;
                _viewsDict.Add(groupsConfig.GroupType, groupView);
            }
        }

        public void UpdateViews(EventCardsSaveData collectionData)
        {
            foreach (var groupView in _viewsDict.Values)
            {
                var totalGroupAmount = collectionData.GetGroupAmount(groupView.GroupType);
                var collectedGroupAmount = collectionData.GetCollectedGroupAmount(groupView.GroupType);
                groupView.UpdateCollectedAmount(collectedGroupAmount, totalGroupAmount);
            }
        }

        public async UniTask CreateGroupViews(List<CardGroupsConfig> groupsData)
        {
            var loadTasks = groupsData.Select(async config => {
                    try 
                    {
                        var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(config.GroupIcon);
                        if (_viewsDict.TryGetValue(config.GroupType, out var view))
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
        
        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (var view in _viewsDict.Values)
            {
                view.OnButtonPressed -= OnButtonPressedHandler;
            }
        }
    }
}