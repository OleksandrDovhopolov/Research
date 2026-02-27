using System;

namespace CardCollection.Core
{
    public struct CardGroupCompletedData
    {
        public string GroupId;
    }
    
    public interface ICardGroupCompletionNotifier
    {
        event Action<CardGroupCompletedData> OnGroupCompleted;
    }
}