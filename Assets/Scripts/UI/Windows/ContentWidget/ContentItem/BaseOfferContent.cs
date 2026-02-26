using System.Collections.Generic;
using CardCollection.Core;

namespace core
{
    public class BaseOfferContent : OfferContent
    {
        public List<GameResource> Resources = new();
        public List<CardPack> CardPack = new();
    }
}