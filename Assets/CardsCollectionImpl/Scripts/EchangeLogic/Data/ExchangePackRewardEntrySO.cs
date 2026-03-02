using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardCollectionImpl
{
    [Serializable]
    public class ResourceRewardData
    {
        public string ResourceId;
        public int Amount;
    }
    
    public class ExchangePackRewardEntrySO : ScriptableObject
    {
        public List<string> CardPacks;
    }

    [CreateAssetMenu(
        fileName = "ExchangePackCardsReward",
        menuName = "Card Collection/Exchange/Rewards/Cards Reward")]
    public class ExchangePackCardsRewardEntrySO : ExchangePackRewardEntrySO
    {
        public List<ResourceRewardData> ResourcesData = new();
    }
}