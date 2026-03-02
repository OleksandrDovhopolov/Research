using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardCollectionImpl
{
    [CreateAssetMenu(
        fileName = "ExchangePacksConfig",
        menuName = "Card Collection/Exchange/Exchange Packs Config")]
    public class ExchangePacksConfig : ScriptableObject
    {
        [SerializeField] private List<ExchangePackEntry> _packs = new();

        public IReadOnlyList<ExchangePackEntry> Packs => _packs;
    }

    [Serializable]
    public class ExchangePackEntry
    {
        public string Id;
        public Sprite Sprite;
        public int PackPrice;
        public ExchangePackCardsRewardEntrySO RewardEntry;
    }
}
