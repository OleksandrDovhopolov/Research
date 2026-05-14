using System;

namespace BattlePass
{
    public interface IBattlePassClaimFlowLock
    {
        IDisposable Acquire();
    }
}
