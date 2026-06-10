using System;
using System.Collections.Generic;
using TMPro;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace Survival
{
    public class LevelUpView : WindowView
    {
        [Serializable]
        public class CardView
        {
            public Image Icon;
            public TMP_Text Title;
            public TMP_Text Description;
            public Button Button;
        }

        [SerializeField] private CardView[] _cards;

        public event Action<int> OnCardClicked;

        protected override void Awake()
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                int index = i;
                _cards[i].Button.onClick.AddListener(() => OnCardClicked?.Invoke(index));
            }
        }

        public void Bind(IReadOnlyList<UpgradeDefinition> choices)
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                bool hasChoice = i < choices.Count && choices[i] != null;
                _cards[i].Button.gameObject.SetActive(hasChoice);
                if (!hasChoice) continue;

                var def = choices[i];
                if (_cards[i].Icon != null)
                {
                    _cards[i].Icon.sprite = def.Icon;
                    _cards[i].Icon.enabled = def.Icon != null;
                }
                if (_cards[i].Title != null) _cards[i].Title.text = def.DisplayName;
                if (_cards[i].Description != null) _cards[i].Description.text = def.Description;
            }
        }

        public void DisableAll()
        {
            for (int i = 0; i < _cards.Length; i++)
                _cards[i].Button.gameObject.SetActive(false);
        }
    }
}
