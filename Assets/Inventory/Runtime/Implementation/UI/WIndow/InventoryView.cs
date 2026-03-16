using System;
using Inventory.API;
using Inventory.Implementation.UI;
using TMPro;
using UIShared;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory.Implementation
{
    public class InventoryView : MonoBehaviour, ICleanup
    {
        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _stackCountTextPro;
        [SerializeField] private Button _openButton;
        [SerializeField] private RectTransform _rectTransform;

        public InventoryItemUiModel InventoryItemUiModel { get; private set; }
        public string ItemId => InventoryItemUiModel.ItemId;
        public bool IsOpenable => InventoryItemUiModel.Category is IOpenable;
        public RectTransform RectTransform => _rectTransform;
        public Sprite Sprite => _image.sprite;

        public event Action<InventoryView> OnOpenableViewClicked;
        
        public void SetData(InventoryItemUiModel model)
        {
            InventoryItemUiModel =  model;

            if (_stackCountTextPro != null)
            {
                _stackCountTextPro.gameObject.SetActive(model.StackCount > 0);
                _stackCountTextPro.text = model.StackCount.ToString();
            }

            RefreshInteractionButtons();
        }

        public void SetSprite(Sprite sprite)
        {
            _image.sprite = sprite;
        }

        public void Cleanup()
        {
            if (_image != null)
            {
                _image.sprite = null;
            }

            if (_stackCountTextPro != null)
            {
                _stackCountTextPro.gameObject.SetActive(false);
                _stackCountTextPro.text = string.Empty;
            }

            if (_openButton != null)
            {
                _openButton.onClick.RemoveAllListeners();
            }
        }

        private void RefreshInteractionButtons()
        {
            if (_openButton == null) return;
            
            _openButton.onClick.RemoveAllListeners();
            _openButton.onClick.AddListener(() =>
            {
                OnOpenableViewClicked?.Invoke(this);
            });
            /*if (IsOpenable)
            {
                _openButton.onClick.AddListener(() =>
                {
                    OnOpenableViewClicked?.Invoke(this);
                });
            }*/
        }

        private void OnDestroy()
        {
            if (_openButton != null)
            {
                _openButton.onClick.RemoveAllListeners();
            }
        }
    }
}
