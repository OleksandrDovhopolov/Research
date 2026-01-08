using System;
using Firebase.Firestore;

namespace core
{
    [Serializable][FirestoreData]
    public class CardProgressData
    {
        [FirestoreProperty] public string CardId { get; set; }
        [FirestoreProperty] public bool IsUnlocked { get; set; }
    
        public CardProgressData() { }
    }
}