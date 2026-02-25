using System.Collections.Generic;
using CardCollection.Core;

namespace core
{
    public class BasePackContent : PackContent
    {
        public List<GameResource> Resources = new();
        public List<CardPack> CardPack = new();
    }
}