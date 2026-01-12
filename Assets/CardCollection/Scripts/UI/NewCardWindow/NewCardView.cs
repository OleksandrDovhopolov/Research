using System;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class NewCardView : WindowView
    {
        [SerializeField] private Button _cardOpenButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        private void OnEnable()
        {
            _canvasGroup.alpha = 1;
        }
    }
}