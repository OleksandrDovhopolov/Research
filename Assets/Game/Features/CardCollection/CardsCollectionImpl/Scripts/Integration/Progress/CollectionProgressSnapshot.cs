using System.Collections.Generic;

namespace CardCollectionImpl
{
    public readonly struct CollectionProgressSnapshot
    {
        public readonly struct GroupProgressSnapshot
        {
            public readonly string GroupType;
            public readonly string GroupName;
            public readonly int CollectedAmount;
            public readonly int TotalAmount;

            public GroupProgressSnapshot(string groupType, string groupName, int collectedAmount, int totalAmount)
            {
                GroupType = groupType;
                GroupName = groupName;
                CollectedAmount = collectedAmount;
                TotalAmount = totalAmount;
            }
        }

        public readonly int CollectedAmount;
        public readonly int TotalAmount;
        public readonly IReadOnlyList<GroupProgressSnapshot> GroupProgress;

        public CollectionProgressSnapshot(
            int collectedAmount,
            int totalAmount,
            IReadOnlyList<GroupProgressSnapshot> groupProgress)
        {
            CollectedAmount = collectedAmount;
            TotalAmount = totalAmount;
            GroupProgress = groupProgress;
        }
    }
}
