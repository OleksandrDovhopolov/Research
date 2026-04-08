using System;
using EventOrchestration.Abstractions;
using Firebase.Database;

namespace EventOrchestration
{
    public class FirebaseClock : IClock, IDisposable
    {
        private long _serverTimeOffsetMs;
        private bool _initialized;
        private readonly DatabaseReference _offsetRef;

        public bool IsReady => _initialized;

        public FirebaseClock()
        {
            _offsetRef = FirebaseDatabase.DefaultInstance
                .GetReference(".info/serverTimeOffset");

            _offsetRef.ValueChanged += HandleOffsetChanged;
        }

        private void HandleOffsetChanged(object sender, ValueChangedEventArgs e)
        {
            if (e.Snapshot?.Value != null)
            {
                _serverTimeOffsetMs = Convert.ToInt64(e.Snapshot.Value);
                _initialized = true;
            }
        }

        public DateTimeOffset UtcNow
        {
            get
            {
                long localNowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return DateTimeOffset.FromUnixTimeMilliseconds(localNowMs + _serverTimeOffsetMs);
            }
        }

        public void Dispose()
        {
            if (_offsetRef != null)
            {
                _offsetRef.ValueChanged -= HandleOffsetChanged;
            }
        }
    }
}