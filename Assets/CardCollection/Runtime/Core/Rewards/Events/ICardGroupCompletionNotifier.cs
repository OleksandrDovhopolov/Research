using System;
using System.Collections.Generic;

namespace CardCollection.Core
{
    public struct CardGroupCompletedData
    {
        public string GroupType;
    }
    
    public readonly struct CardGroupsCompletedData
    {
        public readonly IReadOnlyList<CardGroupCompletedData> Groups;

        public CardGroupsCompletedData(IReadOnlyList<CardGroupCompletedData> groups)
        {
            Groups = groups;
        }
    }
    
    public interface ICardGroupCompletionNotifier
    {
        event Action<CardGroupsCompletedData> OnGroupCompleted;
    }
}