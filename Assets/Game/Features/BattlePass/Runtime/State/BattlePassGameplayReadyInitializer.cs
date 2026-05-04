using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIShared;
using UnityEngine;

namespace BattlePass
{
    public sealed class BattlePassGameplayReadyInitializer : IGameplayReadyInitializer
    {
        private readonly IBattlePassSnapshotStore _battlePassSnapshotStore;
        private bool _isCompleted;

        public BattlePassGameplayReadyInitializer(IBattlePassSnapshotStore battlePassSnapshotStore)
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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[BattlePassGameplayReadyInitializer] Initial Battle Pass sync failed. {exception.Message}");
            }
            finally
            {
                _isCompleted = true;
            }
        }
    }
}
