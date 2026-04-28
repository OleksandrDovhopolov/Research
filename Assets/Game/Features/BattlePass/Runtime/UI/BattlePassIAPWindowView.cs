using System;
using TMPro;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass
{
    public class BattlePassIAPWindowView : WindowView
    {
        [SerializeField] private Button _purchaseButton;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _productText;
        [SerializeField] private TMP_Text _seasonText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _purchaseButtonLabel;

        public event Action PurchaseClick;

        protected override void Awake()
        {
            base.Awake();

            if (_purchaseButton != null)
            {
                _purchaseButton.onClick.AddListener(HandlePurchaseClicked);
            }
        }

        public virtual void ResetView()
        {
            SetTitle(string.Empty);
            SetProduct(string.Empty);
            SetSeason(string.Empty);
            SetStatus(string.Empty);
            SetPurchaseButtonLabel("Verify Purchase");
            SetPurchaseButtonInteractable(true);
        }

        public virtual void SetTitle(string title)
        {
            if (_titleText != null)
            {
                _titleText.text = title ?? string.Empty;
            }
        }

        public virtual void SetProduct(string productId)
        {
            if (_productText != null)
            {
                _productText.text = string.IsNullOrWhiteSpace(productId) ? string.Empty : $"Product: {productId}";
            }
        }

        public virtual void SetSeason(string seasonId)
        {
            if (_seasonText != null)
            {
                _seasonText.text = string.IsNullOrWhiteSpace(seasonId) ? string.Empty : $"Season: {seasonId}";
            }
        }

        public virtual void SetStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status ?? string.Empty;
            }
        }

        public virtual void SetPurchaseButtonLabel(string label)
        {
            if (_purchaseButtonLabel != null)
            {
                _purchaseButtonLabel.text = label ?? string.Empty;
            }
        }

        public virtual void SetPurchaseButtonInteractable(bool isInteractable)
        {
            if (_purchaseButton != null)
            {
                _purchaseButton.interactable = isInteractable;
            }
        }

        protected void RaisePurchaseClick()
        {
            PurchaseClick?.Invoke();
        }

        private void HandlePurchaseClicked()
        {
            RaisePurchaseClick();
        }

        protected override void OnDestroy()
        {
            if (_purchaseButton != null)
            {
                _purchaseButton.onClick.RemoveListener(HandlePurchaseClicked);
            }

            base.OnDestroy();
        }
    }
}
