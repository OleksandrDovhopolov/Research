using System;
using Firebase.Firestore;

namespace CardCollection.Core
{
    [Serializable]
    [FirestoreData]
    public class CardProgressData
    {
        [FirestoreProperty] public string CardId { get; set; }
        [FirestoreProperty] public bool IsUnlocked { get; set; }
        [FirestoreProperty] public bool IsNew { get; set; }

        public CardProgressData() { }
    }
}