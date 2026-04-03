using System;

namespace CardCollection.Core
{
    [Serializable]
    public class CardProgressData
    {
        public string CardId { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsNew { get; set; }

        public CardProgressData() { }
    }
}