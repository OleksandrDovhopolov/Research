using System;
using UnityEngine;

namespace core
{
    [Serializable]
    public class CardModel
    {
        public Sprite Sprite;
        public string CardName;
        public bool Collected;
        public int StarsCount;

        public CardModel(Sprite sprite, string cardName, bool collected, int starsCount)
        {
            Sprite = sprite;
            CardName = cardName;
            Collected = collected;
            StarsCount = starsCount;
        }
    }
}