using System;
using System.Collections.Generic;
using Infrastructure;
using UnityEngine;

namespace BattlePass
{
    public sealed class BattlePassXpPresentationTracker : IBattlePassXpPresentationTracker
    {
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly Dictionary<string, PresentedState> _presentedStateBySeason = new(StringComparer.Ordinal);

        private string _activePlayerId;

        public BattlePassXpPresentationTracker(IPlayerIdentityProvider playerIdentityProvider)
        {
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
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
                Mathf.Max(0, level),
                Mathf.Max(0, xp));
        }

        public void CommitPresented(string seasonId, int level, int xp)
        {
            EnsurePlayerContext();
            if (string.IsNullOrWhiteSpace(seasonId))
            {
                return;
            }

            _presentedStateBySeason[seasonId] = new PresentedState(
                Mathf.Max(0, level),
                Mathf.Max(0, xp));
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
            var currentPlayerId = _playerIdentityProvider.GetPlayerId();
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
