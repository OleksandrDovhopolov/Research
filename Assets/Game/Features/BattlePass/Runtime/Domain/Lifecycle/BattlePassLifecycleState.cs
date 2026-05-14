using System;

namespace BattlePass
{
    public enum BattlePassLifecycleStatus
    {
        Inactive = 0,
        Upcoming = 1,
        Active = 2
    }

    public interface IBattlePassLifecycleState
    {
        BattlePassLifecycleStatus CurrentStatus { get; }
        bool IsActive { get; }
        event Action Changed;
    }

    public sealed class BattlePassLifecycleState : IBattlePassLifecycleState
    {
        public BattlePassLifecycleStatus CurrentStatus { get; private set; } = BattlePassLifecycleStatus.Inactive;
        public bool IsActive => CurrentStatus == BattlePassLifecycleStatus.Active;
        public event Action Changed;

        public void SetStatus(BattlePassLifecycleStatus status)
        {
            if (CurrentStatus == status)
            {
                return;
            }

            CurrentStatus = status;
            Changed?.Invoke();
        }
    }
}
