using System;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace Survival
{
    public class GameOverView : WindowView
    {
        [SerializeField] private Button _restartButton;

        public event Action OnRestartClicked;

        protected override void Awake()
        {
            _restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
        }
    }
}
