using System;

namespace core
{
    [Serializable]
    public class NewCardDisplayData
    {
        public CardCollectionConfig Config { get; }
        public bool IsUnlocked { get; }
        public bool IsNew { get; }
        
        public NewCardDisplayData(CardCollectionConfig config, bool isUnlocked, bool isNew)
        {
            Config = config;
            IsUnlocked = isUnlocked;
            IsNew = isNew;
        }
    }
}
