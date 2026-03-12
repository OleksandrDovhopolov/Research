using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        private CancellationToken _destroyCt;
        private IOpenable _openable;
        
        public string ItemId { get; private set; }

        private void Awake()
        {
            _destroyCt = this.GetCancellationTokenOnDestroy();
        }
        
        public void SetData(InventoryItemUiModel model)
        {
            ItemId = model.ItemId;
            _openable = model.Category as IOpenable;

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
            _openable = null;

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
                if (_openable != null)
                {
                    _openButton.onClick.AddListener(() => InvokeOpenAsync(_destroyCt).Forget());
                }
            }
        }

        private async UniTaskVoid InvokeOpenAsync(CancellationToken cancellationToken)
        {
            if (_openable == null)
            {
                return;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _openable.OpenAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[InventoryView] Failed to open item '{ItemId}'. {exception}");
            }
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
