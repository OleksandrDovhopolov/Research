using System;

namespace CardCollection.Core
{
    public class CardPack
    {
        public CardPackConfig config;
        public int purchaseCount { get; private set; } 

        public CardPack(CardPackConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void OnPurchased()
        {
            purchaseCount++;
        }

        public override string ToString()
        {
            return $"Pack: {config.packName} ({config.cardCount} cards)";
        }
    }
}