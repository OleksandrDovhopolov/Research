using System;
using Infrastructure;

namespace BattlePass
{
    public sealed class BattlePassPlayerContextAdapter : IBattlePassPlayerContext
    {
        private readonly IPlayerIdentityProvider _playerIdentityProvider;

        public BattlePassPlayerContextAdapter(IPlayerIdentityProvider playerIdentityProvider)
        {
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
        }

        public string GetPlayerId()
        {
            return _playerIdentityProvider.GetPlayerId();
        }
    }
}
