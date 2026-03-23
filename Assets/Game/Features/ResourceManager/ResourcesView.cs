using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CoreResources
{
    public class ResourcesView : MonoBehaviour
    {
        [SerializeField] private float _amountChangeDurationSeconds = 0.4f;
        [SerializeField] private Text _goldAmountText;
        [SerializeField] private Text _energyAmountText;
        [SerializeField] private Text _gemsAmountTest;

        private readonly Dictionary<ResourceType, Tween> _tweenByType = new();

        public ResourceManager ResourceManager { get; private set; }

        public void InitView(ResourceManager resourceManager)
        {
            if (ResourceManager != null) return;
            
            ResourceManager = resourceManager;
            
            if (ResourceManager == null) return;
            
            ResourceManager.ResourceAmountChanged += OnResourceAmountChangedHandler;
        }
        public void UpdateFromResourceManager(bool instant = false)
        {
            if (ResourceManager == null) return;

            UpdateResourceAmount(ResourceType.Gold, ResourceManager.Get(ResourceType.Gold), instant);
            UpdateResourceAmount(ResourceType.Energy, ResourceManager.Get(ResourceType.Energy), instant);
            UpdateResourceAmount(ResourceType.Gems, ResourceManager.Get(ResourceType.Gems), instant);
        }
        
        public void UpdateResourceAmount(ResourceType resourceType, int amount, bool instant)
        {
            var amountLabel = GetText(resourceType);
            if (amountLabel == null)
            {
                return;
            }

            KillTween(resourceType);

            if (instant || _amountChangeDurationSeconds <= 0f)
            {
                amountLabel.text = amount.ToString();
                return;
            }

            var hasCurrentValue = int.TryParse(amountLabel.text, out var currentValue);
            var fromValue = hasCurrentValue ? currentValue : amount;
            if (fromValue == amount)
            {
                amountLabel.text = amount.ToString();
                return;
            }

            _tweenByType[resourceType] = DOVirtual.Int(fromValue, amount, _amountChangeDurationSeconds,
                    value => amountLabel.text = value.ToString())
                .SetTarget(this)
                .OnComplete(() => _tweenByType.Remove(resourceType));
        }

        private Text GetText(ResourceType resourceType)
        {
            return resourceType switch
            {
                ResourceType.Gold => _goldAmountText,
                ResourceType.Energy => _energyAmountText,
                ResourceType.Gems => _gemsAmountTest,
                _ => null
            };
        }

        private void KillTween(ResourceType resourceType)
        {
            if (_tweenByType.TryGetValue(resourceType, out var tween))
            {
                tween.Kill();
                _tweenByType.Remove(resourceType);
            }
        }

        private void OnDestroy()
        {
            if (ResourceManager != null)
            {
                ResourceManager.ResourceAmountChanged -= OnResourceAmountChangedHandler;
            }

            foreach (var tween in _tweenByType.Values)
            {
                tween.Kill();
            }

            _tweenByType.Clear();
        }

        private void OnResourceAmountChangedHandler(ResourceType resourceType, int newAmount)
        {
            UpdateResourceAmount(resourceType, newAmount, false);
        }
    }
}