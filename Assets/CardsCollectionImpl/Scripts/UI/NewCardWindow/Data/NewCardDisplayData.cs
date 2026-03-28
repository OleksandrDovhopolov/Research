using System;
using CardCollection.Core;
using Infrastructure;

namespace CardCollectionImpl
{
    [Serializable]
    public class NewCardDisplayData
    {
        public CardConfig Config { get; }
        public bool IsUnlocked { get; }
        public bool IsNew { get; }
        public int DuplicatePoints { get; }
        
        public NewCardDisplayData(CardConfig config, bool isUnlocked, bool isNew, int duplicatePoints = 0)
        {
            Config = config;
            IsUnlocked = isUnlocked;
            IsNew = isNew;
            DuplicatePoints = duplicatePoints;
        }
    }
}
