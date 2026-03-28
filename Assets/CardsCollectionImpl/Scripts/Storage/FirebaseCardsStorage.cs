using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
//using Firebase;
//using Firebase.Auth;
//using Firebase.Firestore;
using UnityEngine;

namespace CardCollectionImpl
{
    public class FirebaseCardsStorage : IEventCardsStorage
    {
        /*private FirebaseAuth _auth;
        private FirebaseFirestore _db;*/
        
        private string _userId;
        
        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException("not implemented");
            /*var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            ct.ThrowIfCancellationRequested();

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
                    ct.ThrowIfCancellationRequested();
                    _userId = result.User.UserId;
                }
                catch (OperationCanceledException)
                {
                    throw;
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

            _db = FirebaseFirestore.DefaultInstance;*/
        }
        
        /*private DocumentReference GetEventDoc(string eventId)
        {
            return _db
                .Collection("users")
                .Document(_userId)
                .Collection("events")
                .Document(eventId);
        }*/
        
        public async UniTask<EventCardsSaveData> LoadAsync(string eventId, CancellationToken ct = default)
        {
            throw new NotImplementedException("not implemented");
            /*var docRef = GetEventDoc(eventId);
            var snapshot = await docRef.GetSnapshotAsync();
            ct.ThrowIfCancellationRequested();

            if (!snapshot.Exists)
            {
                Debug.LogWarning($"[Firebase] No save for event {eventId}, creating empty");
                return new EventCardsSaveData { EventId = eventId };
            }

            return snapshot.ConvertTo<EventCardsSaveData>();*/
        }

        public async UniTask SaveAsync(EventCardsSaveData data, CancellationToken ct = default)
        {
            throw new NotImplementedException("not implemented");
            /*var docRef = GetEventDoc(data.EventId);

            await docRef.SetAsync(data);
            ct.ThrowIfCancellationRequested();

            Debug.LogWarning($"[Firebase] Saved event {data.EventId}");*/
        }

        public UniTask UnlockCardsAsync(EventCardsSaveData data, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            throw new NotImplementedException("not implemented");
        }

        public UniTask ClearCollectionAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException("not implemented");
        }

        public void Dispose()
        {
            /*_auth = null;
            _db = null;*/
        }
    }
}
