using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BattlePass
{
    public sealed class BattlePassStartupSync : IBattlePassStartupSync
    {
        private readonly IBattlePassSnapshotStore _battlePassSnapshotStore;
        private bool _isCompleted;

        public BattlePassStartupSync(IBattlePassSnapshotStore battlePassSnapshotStore)
        {
            _battlePassSnapshotStore = battlePassSnapshotStore ?? throw new ArgumentNullException(nameof(battlePassSnapshotStore));
        }

        public async UniTask InitializeAsync(CancellationToken ct)
        {
            if (_isCompleted)
            {
                return;
            }

            try
            {
                await _battlePassSnapshotStore.RefreshAsync(ct, force: true);
            }
            finally
            {
                _isCompleted = true;
            }
        }
    }
}
