using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIShared;
using UnityEngine;

namespace BattlePass
{
    public sealed class BattlePassGameplayReadyInitializer : IGameplayReadyInitializer
    {
        private readonly IBattlePassStartupSync _startupSync;

        public BattlePassGameplayReadyInitializer(IBattlePassStartupSync startupSync)
        {
            _startupSync = startupSync ?? throw new ArgumentNullException(nameof(startupSync));
        }

        public async UniTask InitializeAsync(CancellationToken ct)
        {
            try
            {
                await _startupSync.InitializeAsync(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[BattlePassGameplayReadyInitializer] Initial Battle Pass sync failed. {exception.Message}");
            }
        }
    }
}
