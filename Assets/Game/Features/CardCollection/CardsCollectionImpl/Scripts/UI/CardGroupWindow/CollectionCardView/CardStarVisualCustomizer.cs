using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    [Serializable]
    public class CardStarTierVisualSettings
    {
        public Color GlowColor;
        public Color GradientColor;
        public Sprite CardSpriteBackground;
    }

    public class CardStarVisualCustomizer : MonoBehaviour
    {
        [SerializeField] private List<CardStarTierVisualSettings> _starTierSettings;
        [SerializeField] private Image _cardSpriteBackground;
        [SerializeField] private Image _glowColor;
        [SerializeField] private Image _gradientColor;
        [SerializeField] private Image _premiumCardFocus;

        public void ApplyStarTier(int starsCount, bool premiumCard)
        {
            var index = Mathf.Clamp(starsCount, 1, 5) - 1;
            if (_starTierSettings == null || index < 0 || index >= _starTierSettings.Count)
            {
                return;
            }

            var settings = _starTierSettings[index];

            if (_cardSpriteBackground != null)
            {
                _cardSpriteBackground.sprite = settings.CardSpriteBackground;
            }

            if (_glowColor != null)
            {
                _glowColor.color = settings.GlowColor;
            }

            if (_gradientColor != null)
            {
                _gradientColor.color = settings.GradientColor;
            }

            _premiumCardFocus.gameObject.SetActive(premiumCard);
        }
    }
}
