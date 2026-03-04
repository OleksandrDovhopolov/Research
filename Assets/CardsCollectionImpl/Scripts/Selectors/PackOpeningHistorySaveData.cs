using System;
using System.Collections.Generic;

namespace CardCollectionImpl
{
    [Serializable]
    public class PackOpeningHistorySaveData
    {
        public List<PackOpeningHistoryEntrySaveData> Entries { get; set; } = new();
    }

    [Serializable]
    public class PackOpeningHistoryEntrySaveData
    {
        public string PackId { get; set; }
        public int ConsecutivePacksWithoutMissingCard { get; set; }
        public int TotalPacksOpened { get; set; }
    }
}
