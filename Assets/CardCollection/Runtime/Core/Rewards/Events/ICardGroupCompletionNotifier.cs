using System;

namespace CardCollection.Core
{
    public struct CardGroupCompletedData
    {
        public string GroupType;
    }
    
    public interface ICardGroupCompletionNotifier
    {
        event Action<CardGroupCompletedData> OnGroupCompleted;
    }
}