using System;
using System.Collections.Generic;

namespace CardCollection.Core
{
    [Serializable]
    public class EventCardsSaveData
    {
        public string EventId { get; set; }
        public int Version { get; set; }
        public int Points { get; set; }
        public List<CardProgressData> Cards { get; set; } = new List<CardProgressData>();

        public EventCardsSaveData() { }
    }
}