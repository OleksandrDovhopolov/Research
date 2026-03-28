using System;

namespace CardCollection.Core
{
    [Serializable]
    public class CardConfig
    {
        public string id;
        public string cardName;
        public string groupType;
        public int stars;
        public bool premiumCard;
        public string icon;
    }
}