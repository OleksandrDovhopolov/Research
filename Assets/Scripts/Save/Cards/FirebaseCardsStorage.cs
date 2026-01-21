using System;
using System.Collections.Generic;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

namespace core
{
    public class FirebaseCardsStorage : IEventCardsStorage
    {
        private FirebaseAuth _auth;
        private FirebaseFirestore _db;
        
        private string _userId;
        
        public async UniTask InitializeAsync()
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
            {
                Debug.LogError($"[Firebase] Dependencies error: {status}");
                return;
            }

            _auth = FirebaseAuth.DefaultInstance;

            if (_auth.CurrentUser == null)
            {
                try
                {
                    var result = await _auth.SignInAnonymouslyAsync();
                    _userId = result.User.UserId;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Firebase] Anonymous sign-in failed: {e}");
                    return;
                }
            }
            else
            {
                _userId = _auth.CurrentUser.UserId;
            }

            _db = FirebaseFirestore.DefaultInstance;
        }
        
        private DocumentReference GetEventDoc(string eventId)
        {
            return _db
                .Collection("users")
                .Document(_userId)
                .Collection("events")
                .Document(eventId);
        }
        
        public async UniTask<EventCardsSaveData> LoadAsync(string eventId)
        {
            var docRef = GetEventDoc(eventId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                Debug.LogWarning($"[Firebase] No save for event {eventId}, creating empty");
                return new EventCardsSaveData { EventId = eventId };
            }

            return snapshot.ConvertTo<EventCardsSaveData>();
        }

        public async UniTask SaveAsync(EventCardsSaveData data)
        {
            var docRef = GetEventDoc(data.EventId);

            await docRef.SetAsync(data);

            Debug.LogWarning($"[Firebase] Saved event {data.EventId}");
        }

        public UniTask UnlockCardsAsync(string eventId, IReadOnlyCollection<string> cardIds)
        {
            throw new NotImplementedException("not implemented");
        }

        public UniTask ClearCollectionAsync()
        {
            throw new NotImplementedException("not implemented");
        }
    }
}