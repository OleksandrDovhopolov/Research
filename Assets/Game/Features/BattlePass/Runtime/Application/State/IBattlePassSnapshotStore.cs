using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BattlePass
{
    public interface IBattlePassSnapshotStore
    {
        event Action<BattlePassSnapshot> SnapshotChanged;

        bool IsInitialized { get; }
        bool HasSnapshot { get; }
        bool LastSyncFailed { get; }
        BattlePassSnapshot CurrentSnapshot { get; }
        DateTimeOffset LastSyncUtc { get; }
        DateTimeOffset LastOpenRefreshUtc { get; }

        bool IsStale(DateTimeOffset nowUtc);
        UniTask RefreshAsync(CancellationToken ct, bool force = false);
        void ReplaceSnapshot(BattlePassSnapshot snapshot);
        bool TryApplyUserState(BattlePassUserState updatedUserState);
    }
}
