using System;
using Infrastructure;

namespace CardCollectionImpl
{
    [Serializable]
    public class NewCardDisplayData
    {
        public CardCollectionConfig Config { get; }
        public bool IsUnlocked { get; }
        public bool IsNew { get; }
        public int DuplicatePoints { get; }
        
        public NewCardDisplayData(CardCollectionConfig config, bool isUnlocked, bool isNew, int duplicatePoints = 0)
        {
            Config = config;
            IsUnlocked = isUnlocked;
            IsNew = isNew;
            DuplicatePoints = duplicatePoints;
        }
    }
}
