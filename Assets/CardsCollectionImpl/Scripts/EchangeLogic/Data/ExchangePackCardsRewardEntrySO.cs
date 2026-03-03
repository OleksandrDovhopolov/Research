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

    [CreateAssetMenu(
        fileName = "ExchangePackCardsReward",
        menuName = "Card Collection/Exchange/Rewards/Cards Reward")]
    public class ExchangePackCardsRewardEntrySO : ScriptableObject
    {
        public List<string> CardPacks;
        public List<ResourceRewardData> ResourcesData = new();
    }
}