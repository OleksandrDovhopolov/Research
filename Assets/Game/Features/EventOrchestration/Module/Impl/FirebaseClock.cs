using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EventOrchestration.Abstractions;

namespace EventOrchestration.Infrastructure
{
    public class FirebaseClock : IClock, IDisposable
    {
        private long _serverTimeOffsetMs = 0;
        /*private DatabaseReference _offsetRef;

        public FirebaseClock()
        {
            // Ссылка на специальный узел Firebase для получения офсета
            _offsetRef = FirebaseDatabase.DefaultInstance.GetReference(".info/serverTimeOffset");
            _offsetRef.ValueChanged += HandleOffsetChanged;
        }

        private void HandleOffsetChanged(object sender, ValueChangedEventArgs e)
        {
            if (e.Snapshot.Value != null)
            {
                _serverTimeOffsetMs = (long)e.Snapshot.Value;
            }
        }

        // Реализация интерфейса IClock
        public DateTimeOffset UtcNow 
        {
            get
            {
                // Берем локальное время и корректируем его на офсет от Firebase
                long localNowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return DateTimeOffset.FromUnixTimeMilliseconds(localNowMs + _serverTimeOffsetMs);
            }
        }
*/
        public void Dispose()
        {
            /*if (_offsetRef != null)
            {
                _offsetRef.ValueChanged -= HandleOffsetChanged;
            }*/
        }

        public DateTimeOffset UtcNow { get; }
    }
}