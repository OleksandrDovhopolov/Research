using System;
using System.Collections.Generic;

namespace CardCollection.Core
{
    [Serializable]
    public class CardPackConfig
    {
        public string packId;
        public string packName;
        public int cardCount;
        public int softCurrencyCost;
        public int hardCurrencyCost;
        public List<string> availableCardRarities;
    }
}