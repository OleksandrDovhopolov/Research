using System;
using System.Collections.Generic;
using Firebase.Firestore;

namespace core
{
    [Serializable][FirestoreData]
    public class EventCardsSaveData
    {
        [FirestoreProperty]public string EventId { get; set; }
        [FirestoreProperty] public int Version { get; set; }
        [FirestoreProperty] public List<CardProgressData> Cards { get; set; } = new List<CardProgressData>();
        
        public EventCardsSaveData() { }
    }
}