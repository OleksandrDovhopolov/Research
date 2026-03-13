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

        //private CancellationToken _destroyCt;
        private InventoryItemUiModel _inventoryItemUiModel;

        public string ItemId => _inventoryItemUiModel.ItemId;
        public bool IsOpenable => _inventoryItemUiModel.Category is IOpenable;
        public RectTransform RectTransform => _rectTransform;

        public event Action<InventoryView> OnOpenableViewClicked;
        
        /*private void Awake()
        {
            _destroyCt = this.GetCancellationTokenOnDestroy();
        }*/
        
        public void SetData(InventoryItemUiModel model)
        {
            _inventoryItemUiModel =  model;

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
            if (_openButton != null)
            {
                _openButton.onClick.RemoveAllListeners();
                if (IsOpenable)
                {
                    _openButton.onClick.AddListener(() =>
                    {
                        OnOpenableViewClicked?.Invoke(this);
                        //InvokeOpenAsync(_destroyCt).Forget();
                    });
                }
            }
        }

        /*private async UniTaskVoid InvokeOpenAsync(CancellationToken cancellationToken)
        {
            if (IsOpenable)
            {
                return;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                OnOpenableViewClicked?.Invoke(_inventoryItemUiModel);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[InventoryView] Failed to open item '{ItemId}'. {exception}");
            }
        }*/

        private void OnDestroy()
        {
            if (_openButton != null)
            {
                _openButton.onClick.RemoveAllListeners();
            }
        }
    }
}
