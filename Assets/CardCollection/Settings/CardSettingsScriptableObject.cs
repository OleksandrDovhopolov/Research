using System;
using System.Collections.Generic;
using UnityEngine;

namespace core
{
    [CreateAssetMenu(fileName = "CardSettings", menuName = "Game/Card Settings")]
    public class CardSettingsScriptableObject : ScriptableObject
    {
        [Header("Background for each card group")]
        public List<GroupBackgroundSetting> GroupBackgrounds;
        
        [Header("Sprites for each card")]
        public List<CardSpriteSetting> CardSprites;
        
        /*public Sprite GetBackgroundForGroup(CardCollectionGroups group)
        {
            foreach (var item in GroupBackgrounds)
            {
                if (item.GroupType == group)
                    return item.Background;
            }
            return null;
        }*/
        
        public Sprite GetCardSprite(int cardId)
        {
            foreach (var item in CardSprites)
            {
                if (item.CardId == cardId)
                    return item.Sprite;
            }
            return null;
        }
    }
    
    [Serializable]
    public class GroupBackgroundSetting
    {
        //public CardCollectionGroups GroupType;
        public Sprite Background;
    }

    [Serializable]
    public class CardSpriteSetting
    {
        public int CardId;
        public Sprite Sprite;
    }
}

