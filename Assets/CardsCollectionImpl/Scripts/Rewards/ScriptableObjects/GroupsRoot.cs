using System;
using System.Collections.Generic;

namespace CardCollectionImpl
{
    [Serializable]
    public class GroupJsonData
    {
        public string ID;
        public string groupType;
        public string groupName;
    }

    [Serializable]
    public class GroupsRoot
    {
        public List<GroupJsonData> groups;
    }
}