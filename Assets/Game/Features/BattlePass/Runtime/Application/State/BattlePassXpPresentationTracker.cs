using System;
using System.Collections.Generic;
namespace BattlePass
{
    public sealed class BattlePassXpPresentationTracker : IBattlePassXpPresentationTracker
    {
        private readonly IBattlePassPlayerContext _playerContext;
        private readonly Dictionary<string, PresentedState> _presentedStateBySeason = new(StringComparer.Ordinal);

        private string _activePlayerId;

        public BattlePassXpPresentationTracker(IBattlePassPlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        public bool TryGetBaseline(string seasonId, out int level, out int xp)
        {
            level = 0;
            xp = 0;

            EnsurePlayerContext();
            if (string.IsNullOrWhiteSpace(seasonId))
            {
                return false;
            }

            if (!_presentedStateBySeason.TryGetValue(seasonId, out var state))
            {
                return false;
            }

            level = state.Level;
            xp = state.Xp;
            return true;
        }

        public void InitializeBaseline(string seasonId, int level, int xp)
        {
            EnsurePlayerContext();
            if (string.IsNullOrWhiteSpace(seasonId))
            {
                return;
            }

            if (_presentedStateBySeason.ContainsKey(seasonId))
            {
                return;
            }

            _presentedStateBySeason[seasonId] = new PresentedState(
                Math.Max(0, level),
                Math.Max(0, xp));
        }

        public void CommitPresented(string seasonId, int level, int xp)
        {
            EnsurePlayerContext();
            if (string.IsNullOrWhiteSpace(seasonId))
            {
                return;
            }

            _presentedStateBySeason[seasonId] = new PresentedState(
                Math.Max(0, level),
                Math.Max(0, xp));
        }

        public void Reset(string seasonId = null)
        {
            EnsurePlayerContext();

            if (string.IsNullOrWhiteSpace(seasonId))
            {
                _presentedStateBySeason.Clear();
                return;
            }

            _presentedStateBySeason.Remove(seasonId);
        }

        private void EnsurePlayerContext()
        {
            var currentPlayerId = _playerContext.GetPlayerId();
            if (string.Equals(_activePlayerId, currentPlayerId, StringComparison.Ordinal))
            {
                return;
            }

            _activePlayerId = string.IsNullOrWhiteSpace(currentPlayerId) ? null : currentPlayerId;
            _presentedStateBySeason.Clear();
        }

        private readonly struct PresentedState
        {
            public PresentedState(int level, int xp)
            {
                Level = level;
                Xp = xp;
            }

            public int Level { get; }
            public int Xp { get; }
        }
    }
}
