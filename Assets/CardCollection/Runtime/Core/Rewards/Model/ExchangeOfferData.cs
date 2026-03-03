using UnityEngine;

namespace CardCollection.Core
{
    public sealed class ExchangeOfferData
    {
        public string OfferId { get; }
        public Sprite Sprite { get; }
        public int Price { get; }

        public ExchangeOfferData(string offerId, Sprite sprite, int price)
        {
            OfferId = offerId;
            Sprite = sprite;
            Price = price;
        }
    }
}
